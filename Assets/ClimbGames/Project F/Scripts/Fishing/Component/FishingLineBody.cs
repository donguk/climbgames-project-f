using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public enum SegmentKind
    {
        Line,   // 일반 줄 점
        Tip,    // 로드 끝 (항상 0번)
        Bobber, // 찌
        Sinker, // 봉돌
        Hook,   // 바늘 (항상 마지막)
        Handle, // 손 (피칭으로 잡고 있을 때)
    }

    // 줄 점에 연결된 장비(찌, 봉돌, 바늘)가 줄 계산 중에 자기 점을 다룸
    public interface ISegmentController
    {
        // 스텝 시작: 질량, 저항 같은 속성 설정
        void OnPreStep(FishingLineBody.Segment segment);

        // 서브스텝마다 적분 직후 (길이 제약 전): 부력처럼 점의 움직임을 더함
        void OnSubStep(FishingLineBody.Segment segment, float deltaTime);
    }

    // 낚싯줄 물리
    // - 점(Segment)을 팁(0번)부터 바늘(마지막)까지 높이 순으로 둠, 높이 = 줄 끝(바늘)에서 잰 줄 길이, 점 사이 줄 길이 = 높이 차
    // - 장비 점(팁, 손, 찌, 봉돌, 바늘) 사이는 segmentLength 간격의 일반 줄 점으로 채움, 줄 점 높이는 한 번 정하면 바뀌지 않음
    //   줄이 풀리면 팁에서 새 점이 나오고 감기면 팁 쪽 점이 들어감 (팁 구간만 0.5 ~ 1.5 · segmentLength)
    // - 점 사이는 최대 길이 제약: 늘어났을 때만 당김, 실제 강성(stiffness, N)으로 늘어남을 표현 (XPBD)
    //   줄 전체를 한 번에 푸는 직접 풀이(삼중대각)라 가벼운 줄 점과 무거운 찌가 이어져 있어도 수렴함
    // - 점마다 질량, 공기 저항, 물 저항, 땅 마찰을 따로 가짐 (나중에 바람, 물흐름도 점마다 줄 수 있게)
    [Serializable]
    public class FishingLineBody
    {
        public class Segment
        {
            public SegmentKind kind;
            public float height;                // 줄 끝(바늘)에서 잰 줄 길이 (팁은 전체 줄 길이)

            // 속성
            public float mass;                  // 질량 (kg)
            public float drag;                  // 장비 공기 저항: 장비 몫 감속 = drag · v² (1/m), 줄 몫 공기 저항은 줄 굵기로 따로 계산
            public float damping;               // 속도 비례 감쇠: 1초에 줄어드는 속도 비율 (1/s), 작은 흔들림까지 줄임
            public float waterDrag;             // 물 저항: 물에 잠기면 1초에 줄어드는 속도 비율 (1/s)
            public float lineLength;            // 이 점이 맡은 줄 길이 (위아래 구간의 절반씩, m)
            public float groundFriction;        // 땅에 닿았을 때 스텝마다 줄어드는 속도 비율 (0 ~ 1)
            public Transform trackingTarget;    // 있으면 이 위치를 따라가는 고정점 (손, 날아가는 바늘, 물고기, 로드 가이드)
            public ISegmentController controller;

            // 상태
            public Vector3 position;
            public Vector3 previous;
            public float waterHeight = float.NegativeInfinity;  // 아래 수면 높이 (없으면 -Infinity)
            public float groundHeight = float.NegativeInfinity; // 아래 땅 높이 + groundOffset
            public bool inWater;
            public bool onGround;

            internal Vector3 stepStart;
            internal float inverseMass;
            internal float solveInverseMass;    // 길이 제약 풀이에만 쓰는 역질량 (장력이 크면 가벼운 줄 점을 무겁게 해서 튀지 않게)

            // 추적 대상까지 시간을 두고 따라가기 (StartTracking)
            internal Vector3 trackingBlendFrom;
            internal float trackingBlendTime;
            internal float trackingBlendDuration;

            public bool IsLine => kind == SegmentKind.Line;
            public bool IsTip => kind == SegmentKind.Tip;
            public bool IsPinned => IsTip || trackingTarget != null;
            public bool HasWater => float.IsNegativeInfinity(waterHeight) == false;

            // target 을 따라가는 고정점이 됨: 지금 위치에서 target 까지 duration 동안 부드럽게 옮겨감 (0 이면 바로)
            public void StartTracking(Transform target, float duration)
            {
                trackingTarget = target;
                trackingBlendFrom = position;
                trackingBlendTime = 0f;
                trackingBlendDuration = target != null ? Mathf.Max(0f, duration) : 0f;
            }

            // 이번 서브스텝의 추적 위치 (보간 중이면 시작 위치에서 target 쪽으로 smoothstep)
            internal Vector3 GetTrackingPosition(float deltaTime)
            {
                if (trackingBlendDuration <= 0f)
                    return trackingTarget.position;

                trackingBlendTime += deltaTime;
                float t = Mathf.Clamp01(trackingBlendTime / trackingBlendDuration);
                if (t >= 1f)
                    trackingBlendDuration = 0f;

                return Vector3.Lerp(trackingBlendFrom, trackingTarget.position, t * t * (3f - 2f * t));
            }
        }

        private const float MinSpanLength = 0.005f;
        private const float MinMass = 0.000001f;
        private const float AirDensity = 1.2f;          // kg/m³
        private const float AirViscosity = 0.000015f;   // 공기 동점성 계수 (m²/s)
        private const float CastHeight = 50f;
        private const float CastDistance = 100f;
        private const float ContactEpsilon = 0.001f;

        [Header("Line Segment")]
        [SerializeField] private float segmentLength = 0.2f;        // 일반 줄 점 간격 (m)
        [SerializeField] private float lineDensity = 0.0001f;       // 줄 1m 의 질량 (kg/m), 줄 점 질량 = 이 값 · 점 간격
        [SerializeField] private float lineDiameter = 0.0003f;      // 줄 굵기 (m), 공기 저항 계산에 씀
        [SerializeField, Min(0f)] private float lineDamping = 0.5f; // 줄 점 속도 비례 감쇠 (1/s), 0 이면 공기 저항만
        [SerializeField] private float lineWaterDrag = 3f;          // 줄 점 물 저항 (1/s)
        [SerializeField, Range(0f, 1f)] private float lineGroundFriction = 0.2f;

        [Header("Physics")]
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField, Min(1)] private int subStepCount = 8;
        [SerializeField, Min(1)] private int iterationCount = 4;    // 서브스텝마다 길이 제약을 푸는 횟수
        // 장력이 클 때 가벼운 줄 점이 옆으로 튀지 않게, 풀이에서만 질량을 최소 stability · 장력 · Δt² / 점 간격 으로 올림
        // (장력이 작으면 원래 질량 그대로, 클수록 안정적이지만 팽팽한 줄의 옆 흔들림이 조금 느려짐)
        [SerializeField, Min(0f)] private float stability = 2f;

        [Header("Contact")]
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private float waterOffset = 0.01f;

        private readonly List<Segment> segments = new List<Segment>();
        private bool hasWaterContact;

        // 다음 스텝에 서브스텝마다 나눠 적용할 줄 늘림: 그 점과 위 점들(팁 포함) 높이를 같이 올려 그 점 바로 아래 구간만 길어짐
        private readonly List<(Segment segment, float amount)> pendingFeeds = new List<(Segment, float)>();
        private readonly List<(int index, float amount)> feedSteps = new List<(int, float)>();
        private readonly List<float> relaxStretches = new List<float>();   // RelaxStretch 에서 구간마다 늘어난 길이

        // 길이 제약 풀이용 (구간마다)
        private Vector3[] normals = new Vector3[16];
        private float[] lambdas = new float[16];
        private float[] diagonal = new float[16];
        private float[] lower = new float[16];
        private float[] upper = new float[16];
        private float[] rhs = new float[16];
        private float[] deltaLambdas = new float[16];
        private float[] tensions = new float[16];

        public IReadOnlyList<Segment> Segments => segments;
        public int SpanCount => Mathf.Max(0, segments.Count - 1);
        public float SegmentLength => segmentLength;

        // 팁 (로드 끝, 항상 0번)
        public Segment Tip => segments[0];

        // 전체 줄 길이 = 팁 높이 + 아직 적용 안 된 Feed 양 (이번 프레임에 예약한 것까지)
        public float Length
        {
            get
            {
                float length = segments.Count > 0 ? segments[0].height : 0f;
                foreach (var (_, amount) in pendingFeeds)
                    length += amount;
                return length;
            }
        }

        // 구간 j (점 j ~ j+1) 의 줄 길이와 장력 (N)
        public float GetSpanLength(int span) => Mathf.Max(MinSpanLength, segments[span].height - segments[span + 1].height);
        public float GetTension(int span) => span >= 0 && span < SpanCount ? tensions[span] : 0f;

        // 수면에 닿은 일반 줄 점이 수면 위에 얹힘 (물고기를 향해 물속으로 들어가는 줄은 끔)
        public void SetWaterContact(bool isEnabled)
        {
            hasWaterContact = isEnabled;
        }

        // segment 바로 아래 구간에서 줄을 amount 만큼 늘림 (음수면 줄임): 그 점과 위 점들(팁 포함) 높이가 같이 올라가 위쪽 구간 길이는 그대로
        // 다음 Step 에서 서브스텝마다 나눠 적용 (어느 점에서 늘려도 전체 줄 길이 Length 가 amount 만큼 늘어남)
        // 릴 감기/풀기처럼 팁에서 늘릴 땐 Feed(Tip, amount)
        public void Feed(Segment segment, float amount)
        {
            if (segment == null || Mathf.Abs(amount) < 0.0000001f)
                return;

            for (int i = 0; i < pendingFeeds.Count; ++i)
            {
                if (pendingFeeds[i].segment == segment)
                {
                    pendingFeeds[i] = (segment, pendingFeeds[i].amount + amount);
                    return;
                }
            }
            pendingFeeds.Add((segment, amount));
        }

        // 팁부터 segment 까지 늘어난 구간마다 그 구간을 실제 거리만큼 늘려 장력을 없앰 (바로 적용), 늘린 길이 합(= 전체 줄 길이 증가량)을 돌려줌
        // - 구간 j 의 정해진 길이 = 높이[j] − 높이[j+1] 이므로, 구간 j 만 s 늘리려면 0 ~ j 번 점 높이를 s 올림
        // - 손 바로 위 구간부터 팁 쪽으로 올라가며 늘어난 길이를 누적해서 위 점들 높이에 더함
        // - 팁 구간 하나만 늘리면 나머지 구간의 장력이 그대로 남아, 놓는 순간 그 사이 장비(찌 등)가 튕겨 오름
        // - 줄을 늘리는 쪽이라 한 번에 적용해도 튀지 않음, 길어진 구간은 다음 스텝 NormalizeSegments 가 줄 점으로 채움
        public float RelaxStretch(Segment segment)
        {
            int index = segments.IndexOf(segment);
            if (index <= 0)
                return 0f;

            // 높이를 바꾸기 전에 구간마다 늘어난 길이를 먼저 구함 (바꾸면서 구하면 아래 점이 올라간 만큼 위 구간 길이가 줄어 보임)
            relaxStretches.Clear();
            for (int j = 0; j < index; ++j)
            {
                float distance = (segments[j].position - segments[j + 1].position).magnitude;
                relaxStretches.Add(Mathf.Max(0f, distance - GetSpanLength(j)));
            }

            // 점 j 는 j 번 구간부터 아래(손 쪽) 구간들이 늘어난 합만큼 올라감
            float total = 0f;
            for (int j = index - 1; j >= 0; --j)
            {
                total += relaxStretches[j];
                segments[j].height += total;
            }
            return total;
        }

        // 팁 점만 있는 줄로 시작
        public void Build(Vector3 tipPosition, float length)
        {
            segments.Clear();
            pendingFeeds.Clear();
            segments.Add(new Segment() { kind = SegmentKind.Tip, height = length, position = tipPosition, previous = tipPosition });
        }

        // 장비 점 추가 (높이 순 위치에 넣음, 사이 줄 점은 다음 스텝에 맞춰짐)
        // 고정점이면 추적 대상 위치, 아니면 위아래 점 사이 높이 비율 위치(줄 위), 아래가 없으면 위에서 아래로 매달림
        // blendDuration > 0 인 고정점은 줄 위의 그 높이 위치에서 시작해 대상까지 blendDuration 동안 옮겨감 (손이 줄을 잡으러 가듯)
        public Segment AddSegment(SegmentKind kind, float height, Transform trackingTarget = null, float blendDuration = 0f)
        {
            var segment = CreateSegment(kind);
            segment.height = ClampHeight(height);
            segment.trackingTarget = trackingTarget;

            int index = 1;
            while (index < segments.Count && segments[index].height >= segment.height)
                index++;
            segments.Insert(index, segment);

            if (segment.IsPinned && blendDuration <= 0f)
            {
                segment.position = trackingTarget.position;
            }
            else
            {
                Segment above = segments[index - 1];
                if (index + 1 < segments.Count)
                {
                    Segment below = segments[index + 1];
                    float range = Mathf.Max(MinSpanLength, above.height - below.height);
                    segment.position = Vector3.Lerp(above.position, below.position, (above.height - segment.height) / range);
                }
                else
                {
                    segment.position = above.position + Vector3.down * (above.height - segment.height);
                }
            }
            segment.previous = segment.stepStart = segment.position;

            if (trackingTarget != null)
                segment.StartTracking(trackingTarget, blendDuration);
            return segment;
        }

        // 장비 점 제거 (빈 간격은 다음 스텝에 줄 점으로 채워짐)
        public void RemoveSegment(Segment segment)
        {
            if (segment != null && segment.IsTip == false)
                segments.Remove(segment);
        }

        // 장비 점 높이 변경: 줄은 그대로 두고 장비 점이 줄을 따라 미끄러짐
        // (높이만 바꾸면 위아래 구간 길이가 갑자기 바뀌어 가벼운 이웃 줄 점이 무거운 장비 쪽으로 끌려가며 튐)
        public void SetHeight(Segment segment, float height)
        {
            if (segment == null || segment.IsTip)
                return;

            float target = ClampHeight(height);
            float delta = target - segment.height;
            if (Mathf.Abs(delta) < 0.000001f)
                return;

            // 고정점(손 등)은 따라가는 대상이 위치를 정함
            if (segment.IsPinned == false)
                SlideAlongLine(segment, delta);

            segment.height = target;
        }

        // 점을 줄을 따라 delta 만큼 옮김 (양수: 팁 쪽, 음수: 바늘 쪽), 구간마다 정해진 길이 비율로 실제 위치를 보간
        // 이전 위치도 같이 옮겨서 속도가 생기지 않게 함
        void SlideAlongLine(Segment segment, float delta)
        {
            int index = segments.IndexOf(segment);
            if (index < 0)
                return;

            int step = delta > 0f ? -1 : 1;
            float remaining = Mathf.Abs(delta);
            float height = segment.height;
            Vector3 position = segment.position;

            for (int next = index + step; next >= 0 && next < segments.Count && remaining > 0f; next += step)
            {
                Segment neighbor = segments[next];
                float restLength = Mathf.Abs(neighbor.height - height);
                if (restLength <= 0.000001f)
                {
                    position = neighbor.position;
                    continue;
                }

                float t = Mathf.Clamp01(remaining / restLength);
                position = Vector3.Lerp(position, neighbor.position, t);
                remaining -= restLength * t;
                height = neighbor.height;
            }

            Vector3 shift = position - segment.position;
            segment.position += shift;
            segment.previous += shift;
            segment.stepStart = segment.position;
        }

        // 점에서 위(팁 쪽) 점으로 향하는 방향
        public Vector3 GetUpDirection(Segment segment)
        {
            int index = segments.IndexOf(segment);
            if (index <= 0)
                return index == 0 && segments.Count > 1 ? (segments[0].position - segments[1].position).normalized : Vector3.up;

            Vector3 direction = segments[index - 1].position - segment.position;
            return direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.up;
        }

        // 점 위치를 [from, to] 범위로 복사 (그리기용)
        public void CopyPositions(List<Vector3> output, int from, int to)
        {
            for (int i = Mathf.Max(0, from); i <= to && i < segments.Count; ++i)
                output.Add(segments[i].position);
        }

        Segment CreateSegment(SegmentKind kind)
        {
            return new Segment()
            {
                kind = kind,
                mass = lineDensity * segmentLength,
                lineLength = segmentLength,
                damping = lineDamping,
                waterDrag = lineWaterDrag,
                groundFriction = lineGroundFriction,
            };
        }

        float ClampHeight(float height)
        {
            return Mathf.Clamp(height, 0f, Mathf.Max(0f, Length - MinSpanLength));
        }

        // stiffness: 줄을 1 배 늘리는 데 드는 힘 (N)
        public void Step(float deltaTime, Vector3 tipPosition, float stiffness)
        {
            if (segments.Count < 2 || deltaTime <= 0f)
                return;

            NormalizeSegments();
            PrepareFeeds();
            EnsureBuffers();
            ResetLineProperties();

            // 장비는 줄 속성 위에 자기 질량·저항을 더함
            foreach (var segment in segments)
            {
                segment.controller?.OnPreStep(segment);
                segment.inverseMass = segment.IsPinned ? 0f : 1f / Mathf.Max(MinMass, segment.mass);
                segment.stepStart = segment.position;
            }
            UpdateContactHeights();

            stiffness = Mathf.Max(0.0001f, stiffness);

            int stepCount = Mathf.Max(1, subStepCount);
            float subDeltaTime = deltaTime / stepCount;
            for (int s = 0; s < stepCount; ++s)
            {
                float t = (s + 1f) / stepCount;
                ApplyFeeds(1f / stepCount);
                Integrate(subDeltaTime, tipPosition, t);

                foreach (var segment in segments)
                    segment.controller?.OnSubStep(segment, subDeltaTime);

                UpdateSolveMasses(subDeltaTime);
                SolveLength(subDeltaTime, stiffness);

                // 다음 서브스텝 풀이용 질량에 씀
                for (int j = 0; j < SpanCount; ++j)
                    tensions[j] = Mathf.Max(0f, -lambdas[j] / (subDeltaTime * subDeltaTime));
            }

            pendingFeeds.Clear();

            UpdateContactStates();
            ApplyGroundFriction();
        }

        // 모든 점을 줄 기본 속성으로: 질량 = 줄 밀도 · 점이 맡은 줄 길이 (위아래 구간의 절반씩), 장비 공기 저항은 0
        void ResetLineProperties()
        {
            for (int i = 0; i < segments.Count; ++i)
            {
                float upperLength = i > 0 ? GetSpanLength(i - 1) : 0f;
                float lowerLength = i < segments.Count - 1 ? GetSpanLength(i) : 0f;

                Segment segment = segments[i];
                segment.lineLength = (upperLength + lowerLength) * 0.5f;
                segment.mass = lineDensity * segment.lineLength;
                segment.drag = 0f;
                segment.damping = lineDamping;
                segment.waterDrag = lineWaterDrag;
                segment.groundFriction = lineGroundFriction;
            }
        }

        // 장비 점(찌 높이 변경, 손 점 추가·제거)이 바뀐 곳만 정리 (평소에는 아무 점도 바뀌지 않음)
        // - 높이 내림차순 정렬 (팁은 항상 0 번): 장비 점이 줄 점 사이로 지나가도 순서가 맞음
        // - 장비 점과 0.1 · segmentLength 보다 가까운 줄 점은 지움 (장비가 줄을 따라 미끄러져 와서 거의 겹친 뒤 지우므로 구간이 합쳐질 때 느슨함이 거의 없음)
        // - 팁 구간을 포함해 1.5 · segmentLength 보다 긴 간격은 아래 점에서 segmentLength 씩 위로 줄 점을 채움
        void NormalizeSegments()
        {
            float length = Mathf.Max(0.01f, segmentLength);

            for (int i = 1; i < segments.Count; ++i)
            {
                if (segments[i].IsLine == false)
                    segments[i].height = ClampHeight(segments[i].height);
            }

            // 삽입 정렬 (거의 정렬되어 있음)
            for (int i = 2; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                int k = i - 1;
                while (k >= 1 && segments[k].height < segment.height)
                {
                    segments[k + 1] = segments[k];
                    k--;
                }
                segments[k + 1] = segment;
            }

            // 장비 점에 너무 가까운 줄 점 제거
            for (int i = segments.Count - 1; i >= 1; --i)
            {
                Segment segment = segments[i];
                if (segment.IsLine == false)
                    continue;

                bool isNearAbove = segments[i - 1].IsLine == false && segments[i - 1].height - segment.height < length * 0.1f;
                bool isNearBelow = i < segments.Count - 1 && segments[i + 1].IsLine == false && segment.height - segments[i + 1].height < length * 0.1f;
                if (isNearAbove || isNearBelow)
                    segments.RemoveAt(i);
            }

            // 긴 간격 채우기 (팁 구간 포함: 줄 길이는 그대로인데 장비가 내려와 팁 구간이 길어질 때도 채움)
            for (int i = 0; i < segments.Count - 1; ++i)
            {
                while (segments[i].height - segments[i + 1].height > length * 1.5f)
                    InsertLine(i + 1, segments[i + 1].height + length);
            }
        }

        // 이번 스텝의 줄 늘림 준비 (Feed 로 예약된 것만, 팁 몫도 FishingLine 이 Feed(Tip, …) 로 넘김)
        // - 늘리는 점마다 바로 아래에서 줄 점을 넣고 뺌, 서브스텝마다 적용할 점 번호와 양을 정해 둠
        void PrepareFeeds()
        {
            for (int i = 0; i < pendingFeeds.Count; ++i)
                UpdateFeedSegments(pendingFeeds[i].segment, pendingFeeds[i].amount);

            feedSteps.Clear();
            foreach (var (segment, amount) in pendingFeeds)
            {
                int index = segments.IndexOf(segment);
                if (index >= 0)
                    feedSteps.Add((index, amount));
            }
        }

        // 늘리는 점 바로 아래에서 줄 점을 넣고 뺌: 줄이 풀리면 새 점이 나오고, 감기면 그 점 쪽 줄 점이 들어감
        // - 줄 점 높이는 그 점 아래에서는 바뀌지 않음 → 풀려 나간 구간은 모두 정확히 segmentLength
        // - 늘리는 점 바로 아래 구간만 0.5 ~ 1.5 · segmentLength 사이에서 바뀜 (넣는 기준과 빼는 기준을 달리 해서 경계에서 점이 생겼다 사라졌다 하지 않음,
        //   0 근처까지 짧아지면 강성 = stiffness / 길이 가 아주 커져서 튈 수 있음)
        // - 넣기는 지금 간격과 늘린 뒤 간격이 모두 길 때만 (서브스텝 중에도 새 구간 길이가 0.5 · L 이상)
        void UpdateFeedSegments(Segment feed, float amount)
        {
            float length = Mathf.Max(0.01f, segmentLength);
            while (true)
            {
                int index = segments.IndexOf(feed);
                if (index < 0 || index >= segments.Count - 1)
                    return;

                Segment below = segments[index + 1];
                float gap = feed.height - below.height;
                float finalGap = gap + amount;

                if (gap > length * 1.5f && finalGap > length * 1.5f)
                {
                    InsertLine(index + 1, below.height + length);
                    continue;
                }

                if (finalGap < length * 0.5f && below.IsLine)
                {
                    segments.RemoveAt(index + 1);
                    continue;
                }

                return;
            }
        }

        // 서브스텝마다 늘리는 양의 ratio 만큼: 그 점과 위 점들(팁 포함) 높이를 올림 → 그 점 바로 아래 구간만 길어짐
        // (한 번에 바꾸면 줄인 길이만큼 첫 서브스텝에 아래 줄 점이 확 끌려가 지나쳤다 돌아오며 튐)
        void ApplyFeeds(float ratio)
        {
            foreach (var (index, amount) in feedSteps)
            {
                float move = amount * ratio;
                for (int i = 0; i <= index && i < segments.Count; ++i)
                    segments[i].height += move;
            }
        }

        // index 위치(위 점 index - 1 과 아래 점 index 사이)에 줄 점 추가: 위치·이전 위치는 높이 비율로 보간해서 지금 줄 모양과 속도를 이어받음
        void InsertLine(int index, float height)
        {
            Segment above = segments[index - 1];
            Segment below = segments[index];
            float range = Mathf.Max(MinSpanLength, above.height - below.height);
            float t = Mathf.Clamp01((above.height - height) / range);

            var segment = CreateSegment(SegmentKind.Line);
            segment.height = height;
            segment.position = Vector3.Lerp(above.position, below.position, t);
            segment.previous = Vector3.Lerp(above.previous, below.previous, t);
            segment.stepStart = segment.position;
            segments.Insert(index, segment);
        }

        void EnsureBuffers()
        {
            int size = segments.Count;
            if (lambdas.Length >= size)
                return;

            size += 16;
            normals = new Vector3[size];
            lambdas = new float[size];
            diagonal = new float[size];
            lower = new float[size];
            upper = new float[size];
            rhs = new float[size];
            deltaLambdas = new float[size];
            tensions = new float[size];
        }

        // 점마다 땅과 수면 높이를 스텝에 한 번 구함
        void UpdateContactHeights()
        {
            int groundMask = 1 << FishingLayer.Ground;
            int waterMask = 1 << FishingLayer.Water;
            for (int i = 1; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                Vector3 origin = segment.position + Vector3.up * CastHeight;

                segment.groundHeight = Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, CastDistance, groundMask, QueryTriggerInteraction.Ignore)
                    ? groundHit.point.y + groundOffset
                    : float.NegativeInfinity;

                // 물 콜라이더는 트리거이고, 수면 높이는 물 오브젝트의 높이
                segment.waterHeight = Physics.Raycast(origin, Vector3.down, out RaycastHit waterHit, CastDistance, waterMask, QueryTriggerInteraction.Collide)
                    ? waterHit.collider.transform.position.y
                    : float.NegativeInfinity;
            }
        }

        // 공기 저항 + 감쇠를 "1초에 줄어드는 속도 비율" 로 (암시적으로 v /= 1 + 값 · Δt 에 씀)
        // - 줄 몫: 가는 원기둥, 힘 = 0.5 · ρ · Cd · 굵기 · 맡은 길이 · v², Cd = 1 + 10 / Re^(2/3), Re = v · 굵기 / ν
        //   0.3mm 줄은 Re 가 작아서(1 m/s 에 20) Cd 가 2 ~ 7 로 큼 → 느릴수록 속도에 비례하는 저항에 가까워짐
        // - 장비 몫: 장비 질량 · drag · v²
        float GetDragRate(Segment segment, float speed)
        {
            float lineMass = lineDensity * segment.lineLength;
            float equipmentMass = Mathf.Max(0f, segment.mass - lineMass);
            float totalMass = Mathf.Max(MinMass, segment.mass);

            float reynolds = Mathf.Max(0.1f, speed * lineDiameter / AirViscosity);
            float dragCoefficient = 1f + 10f / Mathf.Pow(reynolds, 2f / 3f);
            float lineForce = 0.5f * AirDensity * dragCoefficient * lineDiameter * segment.lineLength;   // · v²
            float equipmentForce = equipmentMass * segment.drag;                                        // · v²

            return (lineForce + equipmentForce) * speed / totalMass + segment.damping;
        }

        // 고정점: 스텝 목표 위치까지 t 비율로
        // 나머지: 속도 + 공기 저항·감쇠 + 물 저항(잠겼을 때) + 중력
        void Integrate(float deltaTime, Vector3 tipPosition, float t)
        {
            Vector3 gravityMove = gravity * deltaTime * deltaTime;
            foreach (var segment in segments)
            {
                Vector3 velocity = segment.position - segment.previous;
                segment.previous = segment.position;

                if (segment.IsTip)
                {
                    segment.position = Vector3.Lerp(segment.stepStart, tipPosition, t);
                    continue;
                }

                if (segment.IsPinned)
                {
                    // StartTracking 으로 옮겨가는 중이면 시간에 맞춰 대상 쪽으로, 아니면 이번 스텝 대상 위치까지 t 비율로
                    segment.position = segment.trackingBlendDuration > 0f
                        ? segment.GetTrackingPosition(deltaTime)
                        : Vector3.Lerp(segment.stepStart, segment.trackingTarget.position, t);
                    continue;
                }

                float speed = velocity.magnitude / deltaTime;
                velocity /= 1f + GetDragRate(segment, speed) * deltaTime;

                // 수면에 얹힌 줄 점도 물 저항을 받음
                if (segment.position.y <= segment.waterHeight + waterOffset)
                    velocity /= 1f + segment.waterDrag * deltaTime;

                segment.position += velocity + gravityMove;
            }
        }

        // 길이 제약 풀이용 질량: 점 양쪽 장력 T 가 크면 m ≥ stability · T · Δt² / 점 간격
        // 가벼운 줄 점에 큰 장력이 걸리면 이웃 구간 방향이 조금만 어긋나도 보정이 옆으로 크게 튀어 수렴하지 못함
        // (줄 점의 옆 흔들림 ω·Δt 가 2 를 넘음), 풀이에서만 질량을 올려 막음 (적분·중력·저항은 원래 질량)
        void UpdateSolveMasses(float deltaTime)
        {
            int spanCount = SpanCount;
            float deltaTime2 = deltaTime * deltaTime;
            for (int i = 0; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                if (segment.inverseMass <= 0f)
                {
                    segment.solveInverseMass = 0f;
                    continue;
                }

                float tension = 0f, spacing = float.MaxValue;
                if (i > 0)
                {
                    tension = Mathf.Max(tension, tensions[i - 1]);
                    spacing = Mathf.Min(spacing, GetSpanLength(i - 1));
                }
                if (i < spanCount)
                {
                    tension = Mathf.Max(tension, tensions[i]);
                    spacing = Mathf.Min(spacing, GetSpanLength(i));
                }

                float stableMass = stability * tension * deltaTime2 / spacing;
                segment.solveInverseMass = 1f / Mathf.Max(1f / segment.inverseMass, stableMass);
            }
        }

        // 구간 최대 길이 제약 (XPBD, 늘어났을 때만 당김: λ ≤ 0)
        // 구간마다 C = 거리 − 줄 길이, 강성 k = stiffness / 줄 길이
        // 이웃 구간이 점을 나눠 가지므로 줄 전체를 삼중대각 연립방정식으로 한 번에 풀고, 늘어나지 않은 구간은 빼고 다시 품
        // 역질량은 풀이용(solveInverseMass)을 씀
        void SolveLength(float deltaTime, float stiffness)
        {
            int spanCount = SpanCount;
            float deltaTime2 = deltaTime * deltaTime;

            for (int j = 0; j < spanCount; ++j)
                lambdas[j] = 0f;

            for (int iteration = 0; iteration < iterationCount; ++iteration)
            {
                for (int j = 0; j < spanCount; ++j)
                {
                    Vector3 offset = segments[j].position - segments[j + 1].position;
                    float distance = offset.magnitude;
                    normals[j] = distance > 0.000001f ? offset / distance : Vector3.up;
                }

                for (int j = 0; j < spanCount; ++j)
                {
                    Segment a = segments[j], b = segments[j + 1];
                    float weightSum = a.solveInverseMass + b.solveInverseMass;
                    float restLength = GetSpanLength(j);
                    float constraint = (a.position - b.position).magnitude - restLength;

                    // 처져 있고 쌓인 장력도 없는 구간은 풀지 않음 (Δλ = 0)
                    if (weightSum <= 0f || (constraint <= 0f && lambdas[j] >= 0f))
                    {
                        diagonal[j] = 1f;
                        lower[j] = upper[j] = rhs[j] = 0f;
                        continue;
                    }

                    float k = stiffness / restLength;
                    float compliance = 1f / (k * deltaTime2);
                    diagonal[j] = weightSum + compliance;
                    lower[j] = j > 0 ? -a.solveInverseMass * Vector3.Dot(normals[j], normals[j - 1]) : 0f;
                    upper[j] = j < spanCount - 1 ? -b.solveInverseMass * Vector3.Dot(normals[j], normals[j + 1]) : 0f;
                    rhs[j] = -constraint - compliance * lambdas[j];
                }

                SolveTridiagonal(spanCount);

                // 줄은 밀지 못함: λ ≤ 0
                for (int j = 0; j < spanCount; ++j)
                {
                    float lambda = Mathf.Min(0f, lambdas[j] + deltaLambdas[j]);
                    deltaLambdas[j] = lambda - lambdas[j];
                    lambdas[j] = lambda;
                }

                for (int i = 0; i < segments.Count; ++i)
                {
                    Segment segment = segments[i];
                    if (segment.solveInverseMass <= 0f)
                        continue;

                    Vector3 move = Vector3.zero;
                    if (i < spanCount)
                        move += normals[i] * deltaLambdas[i];
                    if (i > 0)
                        move -= normals[i - 1] * deltaLambdas[i - 1];
                    segment.position += move * segment.solveInverseMass;
                }

                ClampContacts();
            }
        }

        // Thomas 알고리즘: lower[j] · x[j-1] + diagonal[j] · x[j] + upper[j] · x[j+1] = rhs[j]
        void SolveTridiagonal(int count)
        {
            if (count <= 0)
                return;

            // upper, rhs 를 덮어써서 앞으로 소거
            upper[0] /= diagonal[0];
            rhs[0] /= diagonal[0];
            for (int j = 1; j < count; ++j)
            {
                float denominator = diagonal[j] - lower[j] * upper[j - 1];
                if (Mathf.Abs(denominator) < 1e-12f)
                    denominator = 1e-12f;

                upper[j] /= denominator;
                rhs[j] = (rhs[j] - lower[j] * rhs[j - 1]) / denominator;
            }

            deltaLambdas[count - 1] = rhs[count - 1];
            for (int j = count - 2; j >= 0; --j)
                deltaLambdas[j] = rhs[j] - upper[j] * deltaLambdas[j + 1];
        }

        // 땅 아래로 내려가지 않게, 수면 접촉이 켜져 있으면 일반 줄 점은 수면 위에 얹힘
        void ClampContacts()
        {
            for (int i = 1; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                if (segment.IsPinned)
                    continue;

                if (segment.position.y < segment.groundHeight)
                {
                    segment.position.y = segment.groundHeight;
                    segment.previous.y = Mathf.Min(segment.previous.y, segment.groundHeight);
                }

                if (hasWaterContact && segment.IsLine && segment.position.y < segment.waterHeight)
                    segment.position.y = segment.waterHeight;
            }
        }

        void UpdateContactStates()
        {
            for (int i = 1; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                segment.onGround = segment.position.y <= segment.groundHeight + ContactEpsilon;
                segment.inWater = segment.HasWater && segment.position.y <= segment.waterHeight + waterOffset;
            }
        }

        void ApplyGroundFriction()
        {
            for (int i = 1; i < segments.Count; ++i)
            {
                Segment segment = segments[i];
                if (segment.IsPinned == false && segment.onGround)
                    segment.previous = Vector3.Lerp(segment.previous, segment.position, segment.groundFriction);
            }
        }
    }
}
