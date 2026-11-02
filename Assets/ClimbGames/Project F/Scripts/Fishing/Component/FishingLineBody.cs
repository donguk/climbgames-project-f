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

        // 공기저항: 줄을 옆으로 미는 움직임에 걸림
        [Header("Air Drag")]
        [SerializeField] private float airDragLinear = 0.5f;
        [SerializeField] private float airDragQuadratic = 0f;

        // 수면저항: 수면에 닿은 점을 옆으로 미는 움직임에 걸림
        [Header("Surface Drag")]
        [SerializeField] private float surfaceDrag = 3f;
        [SerializeField] private float surfaceStaticSpeed = 0.05f;

        // 물에 들어간 점은 중력 대신 일정한 속도로 천천히 가라앉음
        [Header("Water Sink")]
        [SerializeField] private float sinkSpeed = 0.1f;
        [SerializeField] private float sinkDepth = 0.3f;

        // 공기와 수면 공통: 줄이 길이 방향으로 미끄러지는 움직임에 걸림
        [Header("Line Slide Drag")]
        [SerializeField] private float tangentDrag = 0.2f;

        private const float SurfaceContactOffset = 0.01f;
        private const int PinStretchSegmentCount = 3;

        private List<LinePoint> linePoints = new List<LinePoint>();
        private float forcedTension = 0f;
        private bool isForwardSweep = true;
        private int firstPinIndex = 0;
        private float surfaceHeight = 0f;
        private bool hasSurface = false;

        public IReadOnlyList<LinePoint> Points => linePoints;
        public float VisualLength => linePoints.Sum(x => x.segmentLength);
        public float ForcedTension => forcedTension;
        public float PinStretch { get; private set; }

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

        public void SetWaterSurface(float height)
        {
            surfaceHeight = height;
            hasSurface = true;
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
                    // 세로 방향은 관성 없이 가라앉는 속도만 적용
                    velocity.y = 0f;
                    Vector3 move = ApplyDrag(i, velocity, deltaTime);
                    move.y = -Mathf.Min(sinkSpeed * deltaTime, Mathf.Max(0f, _positions[i].y - (surfaceHeight - sinkDepth)));
                    _positions[i] += move;
                    continue;
                }

                _positions[i] += ApplyDrag(i, velocity, deltaTime) + gravity;
            }

            ApplyConstraint();
            UpdatePinStretch();
        }

        // 첫 고정점 앞 구간이 늘어난 양: 줄이 고정점을 당기는 힘의 추정값
        void UpdatePinStretch()
        {
            PinStretch = 0f;
            if (firstPinIndex >= positionCount)
                return;

            for (int i = Mathf.Max(0, firstPinIndex - PinStretchSegmentCount); i < firstPinIndex; ++i)
            {
                float distance = (_positions[i] - _positions[i + 1]).magnitude;
                PinStretch += Mathf.Max(0f, distance - linePoints[i].segmentLength * (1f - forcedTension));
            }
        }

        bool IsInWater(int index)
        {
            return hasSurface && index < firstPinIndex && _positions[index].y <= surfaceHeight + SurfaceContactOffset;
        }

        // 가라앉는 깊이 아래로는 내려가지 않음: 남는 줄은 더 깊이 가지 않고 옆으로 퍼짐
        void ClampToSinkDepth()
        {
            if (hasSurface == false)
                return;

            float floor = surfaceHeight - sinkDepth;
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
            if (IsInWater(index) && velocity.magnitude / deltaTime < surfaceStaticSpeed)
                return Vector3.zero;

            // i-1 은 이미 갱신되어 prev 에, i+1 은 아직 positions 에 스텝 시작 위치가 있음
            Vector3 tangent = index == positionCount - 1
                ? _positions[index] - _prevPositions[index - 1]
                : _positions[index + 1] - _prevPositions[index - 1];
            tangent = tangent.normalized;

            Vector3 tangentVelocity = tangent * Vector3.Dot(velocity, tangent);
            Vector3 normalVelocity = velocity - tangentVelocity;

            float normalDrag = IsInWater(index)
                ? surfaceDrag
                : airDragLinear + airDragQuadratic * normalVelocity.magnitude / deltaTime;

            return tangentVelocity / (1f + tangentDrag * deltaTime) + normalVelocity / (1f + normalDrag * deltaTime);
        }

        protected override void ApplySegmentLength()
        {
            // 한 방향으로만 풀면 반대쪽 끝까지 보정이 전달되지 않음
            if (isForwardSweep)
            {
                for (int i = 0; i < positionCount - 1; ++i)
                    SolveSegment(i);
            }
            else
            {
                for (int i = positionCount - 2; i >= 0; --i)
                    SolveSegment(i);
            }

            isForwardSweep = !isForwardSweep;
            ClampToSinkDepth();
        }

        void SolveSegment(int i)
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
