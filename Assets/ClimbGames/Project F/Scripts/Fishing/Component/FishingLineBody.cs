using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace ClimbGames.ProjectF
{
    [Serializable]
    public class FishingLineBody : VerletLine
    {
        public class LinePoint
        {
            private static ObjectPool<LinePoint> pool = new ObjectPool<LinePoint>(() => new LinePoint(), (x) => x.Reset());
            public static LinePoint Get() => pool.Get();
            public static void Release(LinePoint instance) => pool.Release(instance);

            public int currentIndex;
            public float segmentLength;
            public bool isDynamic;
            public Transform pinTarget;

            public bool IsPinned => currentIndex == 0 || pinTarget != null;

            public void Reset()
            {
                currentIndex = -1;
                segmentLength = 0f;
                isDynamic = false;
                pinTarget = null;
            }
        }

        [Header("Drag")]
        [SerializeField] private float airDrag = 0.5f; // 공기저항: 줄을 옆으로 미는 움직임에 걸림
        [SerializeField] private float waterDrag = 3f; // 수면저항: 수면에 닿은 점을 옆으로 미는 움직임에 걸림
        [SerializeField] private float tangentDrag = 0.2f; // Slide Drag 줄이 길이 방향으로 미끄러지는 움직임에 걸림

        // 세그먼트에 옆으로 끌린 만큼 속도를 더 줌: 세게 끌린 점일수록 로드가 멈춘 뒤 더 나아감
        [Header("Pull Inertia")]
        [SerializeField, Range(0f, 1f)] private float pullInertia = 0.5f;

        // 팽팽함 기준 길이를 넘은 1m 당 고정점을 당기는 힘
        [Header("Pin Force")]
        [SerializeField] private float lineStiffness = 40f;
        // 줄 길이에서 이 비율만큼 뺀 길이가 팽팽함 기준 길이 (1 이상이면 기준 길이가 0 이하라 항상 끌림)
        // 비율이라서 줄이 짧든 길든 같은 정도로 펴졌을 때 끌기 시작함
        [SerializeField, Range(0f, 10.2f)] private float slackRatio = 0.02f;

        // 물에 들어간 점은 중력 대신 일정한 속도로 천천히 가라앉음
        [Header("Water Sink")]
        [SerializeField] private float sinkSpeed = 0.1f;
        [SerializeField] private float sinkDepth = 0.3f;

        // 줄이 땅 아래로 내려가지 않음
        [Header("Ground")]
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField, Range(0f, 1f)] private float groundFriction = 0.2f;

        private const float WaterContactOffset = 0.01f;
        private const float GroundCastHeight = 50f;
        private const float GroundCastDistance = 100f;
        private const float GroundContactEpsilon = 0.001f;

        private List<LinePoint> linePoints = new List<LinePoint>();
        private float forcedTension = 0f;
        private bool isForwardSweep = true;
        private int firstPinIndex = 0;
        private float waterHeight = 0f;
        private bool hasSurface = false;
        private Vector3[] constraintStartPositions;
        private float[] groundHeights;

        public IReadOnlyList<LinePoint> Points => linePoints;
        public float VisualLength => linePoints.Sum(x => x.segmentLength);
        public float ForcedTension => forcedTension;
        public Vector3 PinForce { get; private set; }

        // 첫 고정점에서 줄을 따라 로드 쪽으로 향하는 방향
        public Vector3 PinDirection { get; private set; }

        // 첫 고정점 근처 줄이 수면과 이루는 각도
        public float PinAngle { get; private set; }

        // 첫 고정점 근처 줄의 수평 속도
        public Vector3 PinLineVelocity { get; private set; }

        public void BuildLine(Vector3 anchorPosition, float length)
        {
            float remainLength = length % segmentLength;
            int segmentCount = (int)(length / segmentLength) + (remainLength > 0f ? 1 : 0);

            // 클리어
            linePoints.ForEach(x => LinePoint.Release(x));
            linePoints.Clear();
            ResizePositionArray(positionCount = segmentCount + 1);

            Vector3 position = anchorPosition;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                // 메타 데이터 생성
                var linePoint = LinePoint.Get();
                linePoint.currentIndex = i;
                if (i < segmentCount)
                    linePoint.segmentLength = i == 0 && remainLength > 0f ? remainLength : segmentLength;
                linePoints.Add(linePoint);

                position.y -= linePoints[i].segmentLength;
            }
        }

        public void SetForcedTension(float tension)
        {
            forcedTension = tension;
        }

        public void SetWaterHeight(float height)
        {
            waterHeight = height;
        }

        public void SetWaterContact(bool isEnabled)
        {
            hasSurface = isEnabled;
        }

        public void Step(float deltaTime, Vector3 anchorPosition)
        {
            _positions[0] = anchorPosition;

            firstPinIndex = positionCount;
            for (int i = 1; i < positionCount; ++i)
            {
                if (linePoints[i].IsPinned)
                {
                    firstPinIndex = i;
                    break;
                }
            }

            Vector3 gravity = gravityForce * deltaTime * deltaTime;
            for (int i = 0; i < positionCount; ++i)
            {
                Vector3 velocity = _positions[i] - _prevPositions[i];
                _prevPositions[i] = _positions[i];

                if (linePoints[i].IsPinned)
                    continue;

                if (IsInWater(i))
                {
                    // 위로 올라가는 속도는 남기고, 내려갈 때는 가라앉는 속도만 적용
                    velocity.y = Mathf.Max(0f, velocity.y);
                    Vector3 move = ApplyDrag(i, velocity, deltaTime);
                    if (move.y <= 0f)
                        move.y = -Mathf.Min(sinkSpeed * deltaTime, Mathf.Max(0f, _positions[i].y - (waterHeight - sinkDepth)));
                    _positions[i] += move;
                    continue;
                }

                _positions[i] += ApplyDrag(i, velocity, deltaTime) + gravity;
            }

            if (constraintStartPositions == null || constraintStartPositions.Length < positionCount)
                constraintStartPositions = new Vector3[_positions.Length];
            Array.Copy(_positions, constraintStartPositions, positionCount);

            UpdateGroundHeights();
            ApplyLengthConstraint();

            // 위치를 바꾸는 보정은 이 앞에, 이전 위치만 바꾸는 보정은 이 뒤에 둠
            UpdatePinState(deltaTime);
            ApplyGroundFriction();
            ApplyPullInertia(deltaTime);
        }

        // 점마다 레이캐스트는 한 번만 하고, 제약 반복 중에는 저장한 높이로 비교함
        void UpdateGroundHeights()
        {
            if (groundHeights == null || groundHeights.Length < positionCount)
                groundHeights = new float[_positions.Length];

            int groundMask = 1 << FishingLayer.Ground;
            for (int i = 0; i < positionCount; ++i)
            {
                Vector3 origin = _positions[i];
                origin.y += GroundCastHeight;

                groundHeights[i] = Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundCastDistance, groundMask, QueryTriggerInteraction.Ignore)
                    ? hit.point.y + groundOffset
                    : float.NegativeInfinity;
            }
        }

        void ClampToGround()
        {
            for (int i = 1; i < positionCount; ++i)
            {
                if (linePoints[i].IsPinned == false && _positions[i].y < groundHeights[i])
                {
                    _positions[i].y = groundHeights[i];
                    _prevPositions[i].y = groundHeights[i];
                }
            }
        }

        bool IsOnGround(int index)
        {
            return _positions[index].y <= groundHeights[index] + GroundContactEpsilon;
        }

        // 땅에 닿은 점의 수평 속도를 줄임
        void ApplyGroundFriction()
        {
            for (int i = 1; i < positionCount; ++i)
            {
                if (linePoints[i].IsPinned == false && IsOnGround(i))
                    _prevPositions[i] = Vector3.Lerp(_prevPositions[i], _positions[i], groundFriction);
            }
        }

        // 제약이 옮긴 거리 중 옆 방향 성분을 다음 스텝 속도에 더함
        // 줄 방향은 튕김과 떨림이 되므로 제외하고, 느린 보정은 무시해서 정지 상태와 미세한 움직임은 그대로 둠
        void ApplyPullInertia(float deltaTime)
        {
            if (pullInertia <= 0f)
                return;

            for (int i = 1; i < positionCount; ++i)
            {
                // 땅이 밀어 올린 보정이 위쪽 속도가 되면 줄이 튐
                if (linePoints[i].IsPinned || IsOnGround(i))
                    continue;

                int next = Mathf.Min(i + 1, positionCount - 1);
                Vector3 tangent = (_positions[next] - _positions[i - 1]).normalized;
                Vector3 correction = _positions[i] - constraintStartPositions[i];
                correction -= tangent * Vector3.Dot(correction, tangent);

                float speed = correction.magnitude / deltaTime;
                if (speed > 0.2f)
                    _prevPositions[i] -= correction * (pullInertia / speed);
            }
        }

        // 고정점을 당기는 힘, 고정점 앞 줄의 각도와 수평 속도를 구함
        // 힘은 줄이 팽팽할 때만 생기므로, 여유 있는 줄의 움직임은 속도로 따로 알려 줌
        void UpdatePinState(float deltaTime)
        {
            PinForce = Vector3.zero;
            PinDirection = Vector3.zero;
            PinAngle = 0f;
            PinLineVelocity = Vector3.zero;
            if (firstPinIndex >= positionCount)
                return;

            const int pinSegmentCount = 1;
            int startIndex = Mathf.Max(0, firstPinIndex - pinSegmentCount);

            // 방향은 고정점 근처 줄에서 구해서 줄의 요동이 반영되게 함
            Vector3 direction = (_positions[startIndex] - _positions[firstPinIndex]).normalized;
            PinDirection = direction;
            float horizontal = new Vector2(direction.x, direction.z).magnitude;
            PinAngle = Mathf.Atan2(direction.y, horizontal) * Mathf.Rad2Deg;

            // 속도 (로드 팁인 0번 점은 제외함)
            Vector3 move = Vector3.zero;
            int moveCount = 0;
            for (int i = Mathf.Max(1, startIndex); i < firstPinIndex; ++i)
            {
                move += _positions[i] - _prevPositions[i];
                moveCount++;
            }

            if (moveCount > 0)
            {
                move.y = 0f;
                PinLineVelocity = move / (moveCount * deltaTime);
            }

            // 힘은 줄 전체가 팽팽함 기준 길이를 넘은 만큼
            PinForce = direction * (lineStiffness * GetOverLength());
        }

        // 팁에서 첫 고정점까지의 직선 거리가 팽팽함 기준 길이를 넘은 길이
        // 기준 길이는 줄 길이에서 처질 수 있는 비율만큼 뺀 길이라서, 줄이 길수록 길어짐
        float GetOverLength()
        {
            float totalRestLength = 0f;
            for (int i = 0; i < firstPinIndex; ++i)
                totalRestLength += linePoints[i].segmentLength * (1f - forcedTension);

            float tautLength = totalRestLength * (1f - slackRatio);
            float straightLength = (_positions[0] - _positions[firstPinIndex]).magnitude;
            return Mathf.Max(0f, straightLength - tautLength);
        }

        bool IsInWater(int index)
        {
            return hasSurface && index < firstPinIndex && _positions[index].y <= waterHeight + WaterContactOffset;
        }

        // 가라앉는 깊이 아래로는 내려가지 않음: 남는 줄은 더 깊이 가지 않고 옆으로 퍼짐
        void ClampToSinkDepth()
        {
            if (hasSurface == false)
                return;

            float floor = waterHeight - sinkDepth;
            for (int i = 1; i < firstPinIndex; ++i)
            {
                if (_positions[i].y < floor)
                {
                    _positions[i].y = floor;
                    _prevPositions[i].y = floor;
                }
            }
        }

        // 줄 방향으로는 잘 미끄러지고, 옆으로 미는 방향은 속도가 빠를수록 저항이 커짐
        Vector3 ApplyDrag(int index, Vector3 velocity, float deltaTime)
        {
            velocity *= dampingFactor;

            // 물속의 정지 마찰: 약한 움직임으로는 물에 들어간 줄이 움직이지 않음
            if (IsInWater(index))
                return Vector3.zero;

            // i-1 은 이미 갱신되어 prev 에, i+1 은 아직 positions 에 스텝 시작 위치가 있음
            Vector3 tangent = index == positionCount - 1
                ? _positions[index] - _prevPositions[index - 1]
                : _positions[index + 1] - _prevPositions[index - 1];
            tangent = tangent.normalized;

            Vector3 tangentVelocity = tangent * Vector3.Dot(velocity, tangent);
            Vector3 normalVelocity = velocity - tangentVelocity;

            float normalDrag = IsInWater(index) ? waterDrag : airDrag;
            return tangentVelocity / (1f + tangentDrag * deltaTime) + normalVelocity / (1f + normalDrag * deltaTime);
        }

        // 길이 제약을 반복해서 풀고, 반복마다 수면과 땅 아래로 내려간 점을 되돌림
        void ApplyLengthConstraint()
        {
            // 마지막 방향이 일정해야 고정점 앞 구간의 측정값이 스텝마다 흔들리지 않음
            isForwardSweep = true;

            for (int i = 0; i < constraintCount; ++i)
            {
                SolveLengthConstraint();
                ClampToSinkDepth();
                ClampToGround();
            }
        }

        // 한 방향으로만 풀면 반대쪽 끝까지 보정이 전달되지 않음
        void SolveLengthConstraint()
        {
            if (isForwardSweep)
            {
                for (int i = 0; i < positionCount - 1; ++i)
                    SolveSegmentLength(i);
            }
            else
            {
                for (int i = positionCount - 2; i >= 0; --i)
                    SolveSegmentLength(i);
            }

            isForwardSweep = !isForwardSweep;
        }

        void SolveSegmentLength(int i)
        {
            Vector3 current = _positions[i];
            Vector3 next = _positions[i + 1];

            LinePoint currentPoint = linePoints[i];
            LinePoint nextPoint = linePoints[i + 1];

            float distance = (current - next).magnitude;
            float segmentLength = currentPoint.segmentLength * (1f - forcedTension);
            float diff = distance - segmentLength;

            Vector3 changeDir = (current - next).normalized;
            Vector3 chagneVector = changeDir * diff;

            if (currentPoint.IsPinned && nextPoint.IsPinned)
            {
                return;
            }
            else if (currentPoint.IsPinned)
            {
                next += chagneVector;
            }
            else if (nextPoint.IsPinned)
            {
                current -= chagneVector;
            }
            else
            {
                current -= chagneVector * 0.5f;
                next += chagneVector * 0.5f;
            }

            _positions[i] = current;
            _positions[i + 1] = next;
        }

        public void SetLength(float length)
        {
            float visualLength = VisualLength;
            if (visualLength < length)
            {
                AddLength(length - visualLength);
            }
            else
            {
                RemoveLength(visualLength - length);
            }
        }

        public float GetLength(int index)
        {
            float length = 0f;
            int count = Mathf.Min(index, linePoints.Count);
            for (int i = 0; i < count; ++i)
                length += linePoints[i].segmentLength;
            return length;
        }

        public bool TryGetPinnedLength(out float length)
        {
            length = 0f;
            for (int i = 0; i < linePoints.Count; ++i)
            {
                length += linePoints[i].segmentLength;
                if (i > 0 && linePoints[i].IsPinned)
                    return true;
            }
            return false;
        }

        void AddLength(float amount)
        {
            if (linePoints.Count > 0)
            {
                var linePoint = linePoints[0];
                float needLength = segmentLength - linePoint.segmentLength;
                linePoint.segmentLength = Mathf.Min(linePoint.segmentLength + amount, segmentLength);
                amount -= needLength;
            }

            if (amount > 0f)
            {
                int addCount = (int)(amount / segmentLength) + 1;
                for (int i = 0; i < addCount; ++i)
                {
                    Vector3 position = GetPosition(0);
                    InsertPosition(0, position, position);

                    var linePoint = LinePoint.Get();
                    linePoint.segmentLength = segmentLength < amount ? segmentLength : amount;
                    linePoints.Insert(0, linePoint);

                    amount -= linePoint.segmentLength;
                }

                for (int i = 0; i < linePoints.Count; ++i)
                    linePoints[i].currentIndex = i;
            }
        }

        void RemoveLength(float amount)
        {
            bool isRemoved = false;
            while (amount > 0f && linePoints.Count > 0)
            {
                var linePoint = linePoints[0];
                if (linePoint.segmentLength <= amount)
                {
                    RemovePosition(0);

                    LinePoint.Release(linePoint);
                    linePoints.RemoveAt(0);

                    amount -= linePoint.segmentLength;
                    isRemoved = true;
                }
                else
                {
                    linePoint.segmentLength -= amount;
                    break;
                }
            }

            if (isRemoved)
            {
                for (int i = 0; i < linePoints.Count; ++i)
                    linePoints[i].currentIndex = i;
            }
        }

        public LinePoint AddPinPoint(FishingLinePoint pinTarget)
        {
            LinePoint pinPoint = null;

            float accumulatedHeight = 0f, pinEpsilon = segmentLength * 0.3f;
            for (int i = linePoints.Count - 2; i >= 0; --i)
            {
                var currentPoint = linePoints[i];
                if (pinTarget.Height < accumulatedHeight + currentPoint.segmentLength || i == 0)
                {
                    Vector3 position = GetPosition(i + 1);
                    Vector3 up = (GetPosition(i) - position).normalized;

                    float dist = Mathf.Clamp(pinTarget.Height - accumulatedHeight, 0f, currentPoint.segmentLength);
                    if (dist < pinEpsilon)
                    {
                        // 1. i + 1 번 점(아래쪽 점)과 거의 일치하는 경우
                        pinPoint = linePoints[i + 1];
                        pinPoint.pinTarget = pinTarget.transform;
                        break;
                    }

                    if (currentPoint.segmentLength - dist < pinEpsilon)
                    {
                        // 2. i 번 점(위쪽 점)과 거의 일치하는 경우
                        pinPoint = currentPoint;
                        pinPoint.pinTarget = pinTarget.transform;
                        break;
                    }

                    position += up * dist;
                    InsertPosition(i + 1, position, position);
                    currentPoint.segmentLength -= dist;

                    pinPoint = LinePoint.Get();
                    pinPoint.segmentLength = dist;
                    pinPoint.pinTarget = pinTarget.transform;
                    pinPoint.isDynamic = true;
                    linePoints.Insert(i + 1, pinPoint);

                    for (int j = i + 1; j < linePoints.Count; ++j)
                        linePoints[j].currentIndex = j;

                    break;
                }

                accumulatedHeight += currentPoint.segmentLength;
            }

            return pinPoint;
        }

        public void RemovePinPoint(LinePoint pinPoint)
        {
            if (pinPoint != null)
                RemovePinPoint(pinPoint.currentIndex);
        }

        public void RemovePinPoint(int pinIndex)
        {
            if (pinIndex >= 0 && pinIndex < linePoints.Count)
            {
                var pinPoint = linePoints[pinIndex];

                pinPoint.pinTarget = null;
                if (pinPoint.isDynamic)
                {
                    // 시작, 끝 점 제외
                    if (pinIndex > 0 && pinIndex < linePoints.Count - 1)
                        linePoints[pinIndex - 1].segmentLength += pinPoint.segmentLength;

                    linePoints.RemoveAt(pinIndex);
                    RemovePosition(pinIndex);

                    for (int i = pinIndex; i < linePoints.Count; ++i)
                        linePoints[i].currentIndex = i;
                }
            }
        }
    }
}
