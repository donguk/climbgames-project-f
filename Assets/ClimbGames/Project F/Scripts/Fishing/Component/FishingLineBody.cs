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

            // 길이 제약에 끌리는 정도 (역질량: 1 은 일반 줄 점, 작을수록 무거움, 고정점은 무시하고 0)
            public float weight;

            // 찌 점: 줄을 만들 때부터 있고 지워지지 않음, 잠기면 부력으로 수면에 떠오름
            public bool isBobber;
            // 몸통이 다 잠겼을 때 위로 미는 가속도 (m/s², 중력 크기보다 커야 뜸)
            public float buoyancy;
            // 잠겼을 때 위아래 움직임을 줄이는 비율 (1/s)
            public float buoyancyDamping;
            // 찌 점이 수면보다 이만큼 높아지면 물에서 빠진 것으로 봄
            public float leaveHeight;
            // 찌 점이 뜨는 높이를 수면에서 옮김 (입질로 찌가 잠길 때 음수)
            public float surfaceOffset;
            // 찌 점이 물에 있을 때 수평으로 움직이는 속도에서 빼는 마찰 속도 (정지 마찰)
            public float waterFriction;
            // 정지 마찰을 넘어 움직이는 수평 속도를 줄이는 비율 (1/s, 물의 저항)
            public float waterDrag;
            // 수면이 찌를 잡는 힘: 줄이 찌를 위로 당기는 속도에서 빼는 값 (m/s)
            public float surfaceHold;

            // 매 스텝 갱신되는 상태
            public float waterHeight;   // 점 아래 수면 높이 (없으면 -Infinity)
            public float groundHeight;  // 점 아래 땅 높이 + groundOffset (없으면 -Infinity)
            public float sinkFloor;     // 가라앉을 수 있는 가장 낮은 높이
            public bool inWater;
            public bool onGround;

            public bool IsPinned => currentIndex == 0 || pinTarget != null;
            public bool HasWater => float.IsNegativeInfinity(waterHeight) == false;

            public void Reset()
            {
                currentIndex = -1;
                segmentLength = 0f;
                isDynamic = false;
                pinTarget = null;

                weight = 1f;
                isBobber = false;
                buoyancy = 0f;
                buoyancyDamping = 0f;
                leaveHeight = 0f;
                surfaceOffset = 0f;
                waterFriction = 0f;
                waterDrag = 0f;
                surfaceHold = 0f;

                waterHeight = float.NegativeInfinity;
                groundHeight = float.NegativeInfinity;
                sinkFloor = float.NegativeInfinity;
                inWater = false;
                onGround = false;
            }
        }

        [Header("Drag")]
        [SerializeField] private float airDrag = 0.5f; // 공기저항: 줄을 옆으로 미는 움직임에 걸림
        [SerializeField] private float waterDrag = 3f; // 수면저항: 수면에 닿은 점을 옆으로 미는 움직임에 걸림
        [SerializeField] private float tangentDrag = 0.2f; // Slide Drag 줄이 길이 방향으로 미끄러지는 움직임에 걸림

        // 세그먼트에 옆으로 끌린 만큼 속도를 더 줌: 세게 끌린 점일수록 로드가 멈춘 뒤 더 나아감
        [Header("Pull Inertia")]
        [SerializeField, Range(0f, 1f)] private float pullInertia = 0.5f;

        // 물에 들어간 점은 중력 대신 일정한 속도로 천천히 가라앉음
        [Header("Water Sink")]
        [SerializeField] private float sinkSpeed = 0.1f;
        [SerializeField] private float sinkDepth = 0.3f;
        // 고정점에서 줄을 따라 1m 떨어질 때 더 가라앉을 수 있는 깊이 (1 미만이어야 줄이 닿을 수 있음)
        [SerializeField, Range(0f, 1f)] private float sinkSlope = 0.5f;

        // 줄이 땅 아래로 내려가지 않음
        [Header("Ground")]
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField, Range(0f, 1f)] private float groundFriction = 0.2f;

        private const float WaterContactOffset = 0.01f;
        private const float GroundCastHeight = 50f;
        private const float GroundCastDistance = 100f;
        private const float GroundContactEpsilon = 0.001f;
        private const float MinSegmentLength = 0.01f;
        // 찌 몸통이 다 잠기는 깊이: 이 깊이까지는 잠긴 만큼 부력이 커지고, 더 깊으면 buoyancy 로 일정함
        private const float BobberFloatDepth = 0.03f;

        private List<LinePoint> linePoints = new List<LinePoint>();
        private bool isForwardSweep = true;
        private int firstPinIndex = 0;
        private bool hasSurface = false;
        private Vector3[] constraintStartPositions;

        public IReadOnlyList<LinePoint> Points => linePoints;
        // 찌 점 (BuildLine 에서 만들고 지우지 않음)
        public LinePoint BobberPoint { get; private set; }
        public float VisualLength => linePoints.Sum(x => x.segmentLength);
        // 길이 제약이 첫 고정점을 옮긴 거리 (고정점에 무게가 있을 때만 0 이 아님)
        public Vector3 PinDisplacement { get; private set; }

        // 첫 고정점에서 줄을 따라 로드 쪽으로 향하는 방향
        public Vector3 PinDirection { get; private set; }

        // 첫 고정점 근처 줄이 수면과 이루는 각도
        public float PinAngle { get; private set; }

        // 첫 고정점 근처 줄의 수평 속도
        public Vector3 PinLineVelocity { get; private set; }

        // 디버그: 로드 팁부터 첫 고정점까지 정해진 길이의 합과, 실제 거리가 그보다 늘어난 길이
        public float PinRestLength { get; private set; }
        public float PinStretch { get; private set; }
        public float LastDeltaTime { get; private set; }

        public void BuildLine(Vector3 anchorPosition, float length, float bobberHeight)
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

            // 찌 점은 끝에서 bobberHeight 만큼 떨어진 곳에 항상 둠
            BobberPoint = InsertPointAtHeight(bobberHeight);
            BobberPoint.isBobber = true;
        }

        public void SetWaterContact(bool isEnabled)
        {
            hasSurface = isEnabled;
        }

        public void Step(float deltaTime, Vector3 anchorPosition)
        {
            _positions[0] = anchorPosition;

            // 첫 고정점 또는 찌 점: 이 앞의 줄만 수면에 걸리고 가라앉음
            firstPinIndex = positionCount;
            for (int i = 1; i < positionCount; ++i)
            {
                if (linePoints[i].IsPinned || linePoints[i].isBobber)
                {
                    firstPinIndex = i;
                    break;
                }
            }

            UpdateSinkFloors();

            Vector3 gravity = gravityForce * deltaTime * deltaTime;
            for (int i = 0; i < positionCount; ++i)
            {
                Vector3 velocity = _positions[i] - _prevPositions[i];
                _prevPositions[i] = _positions[i];

                if (linePoints[i].IsPinned)
                    continue;

                // 수면 아래로 잠긴 찌는 잠긴 깊이만큼 부력을 받고(몸통이 다 잠기면 buoyancy 로 일정), 위아래 움직임은 물이 잡아 줌
                // 줄이 부력보다 세게 당기면 잠긴 채로 끌려가고, 풀리면 수면으로 떠오름
                LinePoint point = linePoints[i];
                if (point.isBobber && point.HasWater)
                {
                    float depth = point.waterHeight + point.surfaceOffset - _positions[i].y;
                    if (depth > 0f)
                    {
                        velocity = ApplyDrag(i, velocity, deltaTime);
                        velocity.y /= 1f + point.buoyancyDamping * deltaTime;

                        float lift = point.buoyancy * Mathf.Clamp01(depth / BobberFloatDepth);
                        _positions[i] += velocity + Vector3.up * ((lift + gravityForce.y) * deltaTime * deltaTime);
                        continue;
                    }
                }

                _positions[i] += ApplyDrag(i, velocity, deltaTime) + gravity;
            }

            if (constraintStartPositions == null || constraintStartPositions.Length < positionCount)
                constraintStartPositions = new Vector3[_positions.Length];
            Array.Copy(_positions, constraintStartPositions, positionCount);

            UpdateContactHeights();
            ApplyLengthConstraint();
            ApplyWaterFriction(deltaTime);
            UpdatePointStates();

            // 위치를 바꾸는 보정은 이 앞에, 이전 위치만 바꾸는 보정은 이 뒤에 둠
            UpdatePinState(deltaTime);
            ApplyGroundFriction();
            ApplyPullInertia(deltaTime);
        }

        // 점마다 땅과 수면 높이를 레이캐스트로 한 번만 구하고, 제약 반복 중에는 저장한 높이로 비교함
        void UpdateContactHeights()
        {
            int groundMask = 1 << FishingLayer.Ground;
            int waterMask = 1 << FishingLayer.Water;
            for (int i = 0; i < positionCount; ++i)
            {
                LinePoint point = linePoints[i];
                Vector3 origin = _positions[i];
                origin.y += GroundCastHeight;

                point.groundHeight = Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, GroundCastDistance, groundMask, QueryTriggerInteraction.Ignore)
                    ? groundHit.point.y + groundOffset
                    : float.NegativeInfinity;

                // 물 콜라이더는 트리거이고, 수면 높이는 물 오브젝트의 높이로 정함
                point.waterHeight = Physics.Raycast(origin, Vector3.down, out RaycastHit waterHit, GroundCastDistance, waterMask, QueryTriggerInteraction.Collide)
                    ? waterHit.collider.transform.position.y
                    : float.NegativeInfinity;
            }
        }

        void ClampToGround()
        {
            for (int i = 1; i < positionCount; ++i)
            {
                LinePoint point = linePoints[i];
                if (point.IsPinned == false && _positions[i].y < point.groundHeight)
                {
                    _positions[i].y = point.groundHeight;
                    _prevPositions[i].y = point.groundHeight;
                }
            }
        }

        bool IsOnGround(int index)
        {
            return _positions[index].y <= linePoints[index].groundHeight + GroundContactEpsilon;
        }

        // 물에 뜬 찌 점
        // - 수평: 정지 마찰(waterFriction)보다 약하게 끌리면 멈춰 있고, 넘는 만큼은 물 저항(waterDrag)으로 줄여서 천천히 따라옴
        // - 위쪽: 줄이 위로 당긴 속도에서 수면이 잡는 속도(surfaceHold)만큼 빼서 들림 (약하게 당기면 수면에 붙어 있음)
        //   부력으로 떠오르는 움직임은 길이 제약이 옮긴 거리가 아니므로 잡지 않음
        void ApplyWaterFriction(float deltaTime)
        {
            LinePoint point = BobberPoint;
            if (point == null || point.IsPinned || point.inWater == false)
                return;

            int index = point.currentIndex;
            Vector3 position = _positions[index];

            // 수평: 정지 마찰과 물 저항
            Vector3 move = position - _prevPositions[index];
            move.y = 0f;
            Vector3 keep = ReduceBy(move, point.waterFriction * deltaTime) / (1f + point.waterDrag * deltaTime);
            position += keep - move;

            // 수면이 잡는 힘: 줄이 위로 당긴 거리
            float lift = position.y - constraintStartPositions[index].y;
            if (lift > 0f)
                position.y -= Mathf.Min(lift, point.surfaceHold * deltaTime);

            _positions[index] = position;
        }

        // 벡터의 길이를 amount 만큼 줄임 (0 아래로는 내려가지 않음)
        static Vector3 ReduceBy(Vector3 vector, float amount)
        {
            float length = vector.magnitude;
            return length > amount ? vector * ((length - amount) / length) : Vector3.zero;
        }

        // 점마다 물과 땅에 닿아 있는지 갱신함
        void UpdatePointStates()
        {
            for (int i = 0; i < positionCount; ++i)
            {
                LinePoint point = linePoints[i];
                point.onGround = IsOnGround(i);

                if (point.isBobber == false)
                {
                    point.inWater = IsInWater(i);
                    continue;
                }

                // 찌 점은 수면에 닿으면 물에 들어가고, leaveHeight 보다 높이 들려야 물에서 빠짐
                if (point.HasWater == false)
                {
                    point.inWater = false;
                    continue;
                }

                float surface = point.waterHeight + point.surfaceOffset;
                point.inWater = point.inWater
                    ? _positions[i].y <= surface + point.leaveHeight
                    : _positions[i].y <= surface + WaterContactOffset;
            }
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
                // 물에 닿은 점(찌 포함)은 물이 움직임을 잡으므로 관성을 더하지 않음
                LinePoint point = linePoints[i];
                if (point.IsPinned || IsOnGround(i) || point.inWater)
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

        // 길이 제약이 고정점을 옮긴 거리, 고정점 앞 줄의 방향, 각도, 수평 속도를 구함
        // 고정점은 줄이 팽팽할 때만 크게 옮겨지므로, 여유 있는 줄의 움직임은 속도로 따로 알려 줌
        void UpdatePinState(float deltaTime)
        {
            PinDisplacement = Vector3.zero;
            PinDirection = Vector3.zero;
            PinAngle = 0f;
            PinLineVelocity = Vector3.zero;
            PinRestLength = 0f;
            PinStretch = 0f;
            LastDeltaTime = deltaTime;
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

            PinDisplacement = _positions[firstPinIndex] - constraintStartPositions[firstPinIndex];

            // 디버그: 팁부터 고정점까지 늘어난 길이
            float restLength = 0f, actualLength = 0f;
            for (int i = 0; i < firstPinIndex; ++i)
            {
                restLength += linePoints[i].segmentLength;
                actualLength += (_positions[i + 1] - _positions[i]).magnitude;
            }
            PinRestLength = restLength;
            PinStretch = actualLength - restLength;
        }

        // 찌(또는 첫 고정점) 앞의 줄 점이 수면에 닿았는지
        bool IsInWater(int index)
        {
            LinePoint point = linePoints[index];
            return hasSurface && index < firstPinIndex && _positions[index].y <= point.waterHeight + WaterContactOffset;
        }

        // 고정점은 움직이지 않고, 나머지는 점의 무게대로 끌림
        float GetWeight(int index)
        {
            LinePoint point = linePoints[index];
            return point.IsPinned ? 0f : point.weight;
        }

        // 찌 근처 점은 줄이 닿을 수 있는 깊이까지만 가라앉게 바닥을 정함
        // 닿을 수 없는 깊이로 내려가려 하면 길이 제약이 매 스텝 되돌리면서 찌를 당김
        void UpdateSinkFloors()
        {
            float distanceToPin = 0f;
            for (int i = Mathf.Min(firstPinIndex, positionCount - 1); i >= 0; --i)
            {
                LinePoint point = linePoints[i];
                if (i < firstPinIndex)
                    distanceToPin += point.segmentLength;

                point.sinkFloor = point.waterHeight - Mathf.Min(sinkDepth, distanceToPin * sinkSlope);
            }
        }

        // 가라앉는 깊이 아래로는 내려가지 않음: 남는 줄은 더 깊이 가지 않고 옆으로 퍼짐
        void ClampToSinkDepth()
        {
            if (hasSurface == false)
                return;

            for (int i = 1; i < firstPinIndex; ++i)
            {
                float floor = linePoints[i].sinkFloor;
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
            //if (IsInWater(index))
            //    return Vector3.zero;

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
                //ClampToSinkDepth();
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

            float distance = (current - next).magnitude;
            float segmentLength = currentPoint.segmentLength;
            float diff = distance - segmentLength;

            Vector3 changeDir = (current - next).normalized;
            Vector3 chagneVector = changeDir * diff;

            // 무게 비율로 나눠 옮김
            float currentWeight = GetWeight(i);
            float nextWeight = GetWeight(i + 1);
            float weightSum = currentWeight + nextWeight;
            if (weightSum <= 0f)
                return;

            current -= chagneVector * (currentWeight / weightSum);
            next += chagneVector * (nextWeight / weightSum);

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

                // 다음 점이 찌나 고정점이면 팁 점을 지우지 않음 (지우면 그 점이 팁이 됨)
                bool isNextKept = linePoints.Count > 1 && (linePoints[1].isBobber || linePoints[1].pinTarget != null);
                if (isNextKept)
                {
                    linePoint.segmentLength = Mathf.Max(MinSegmentLength, linePoint.segmentLength - amount);
                    break;
                }

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
            LinePoint pinPoint = InsertPointAtHeight(pinTarget.Height);
            if (pinPoint != null)
                pinPoint.pinTarget = pinTarget.transform;
            return pinPoint;
        }

        // 줄 끝에서 height 만큼 떨어진 곳의 점을 돌려줌
        // 기존 점과 거의 겹치면 그 점을 쓰고, 아니면 구간을 나눠 새 점(isDynamic)을 끼워 넣음
        LinePoint InsertPointAtHeight(float height)
        {
            float accumulatedHeight = 0f, pinEpsilon = segmentLength * 0.3f;
            for (int i = linePoints.Count - 2; i >= 0; --i)
            {
                var currentPoint = linePoints[i];
                if (height < accumulatedHeight + currentPoint.segmentLength || i == 0)
                {
                    Vector3 position = GetPosition(i + 1);
                    Vector3 up = (GetPosition(i) - position).normalized;

                    float dist = Mathf.Clamp(height - accumulatedHeight, 0f, currentPoint.segmentLength);

                    // 1. i + 1 번 점(아래쪽 점)과 거의 일치하는 경우
                    if (dist < pinEpsilon)
                        return linePoints[i + 1];

                    // 2. i 번 점(위쪽 점)과 거의 일치하는 경우
                    if (currentPoint.segmentLength - dist < pinEpsilon)
                        return currentPoint;

                    position += up * dist;
                    InsertPosition(i + 1, position, position);
                    currentPoint.segmentLength -= dist;

                    var newPoint = LinePoint.Get();
                    newPoint.segmentLength = dist;
                    newPoint.isDynamic = true;
                    linePoints.Insert(i + 1, newPoint);

                    for (int j = i + 1; j < linePoints.Count; ++j)
                        linePoints[j].currentIndex = j;

                    return newPoint;
                }

                accumulatedHeight += currentPoint.segmentLength;
            }

            return null;
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

                // 찌 점은 줄에서 지우지 않음
                if (pinPoint.isDynamic && pinPoint.isBobber == false)
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
