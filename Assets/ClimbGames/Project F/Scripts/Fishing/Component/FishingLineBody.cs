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
            public float bodyDiameter;          // 장비 몸체 지름 (m), 0 이면 몸체 없음 (줄 점)
            public float bodyDragScale = 1f;    // 장비 몸체 구 대비 저항 배율
            public float bodyVolume;            // 장비 몸체 부력 부피 (m³), 질량 / 재질 밀도
            public float bodyHeight;            // 몸체 잠김 비율을 재는 높이 (m), 0 이면 몸체 없음
            public float bodyPivot;             // 점이 몸체 높이의 어디에 있는지 (0 아래 끝 ~ 1 위 끝, 구는 0.5)
            public float damping;               // 속도 비례 감쇠: 1초에 줄어드는 속도 비율 (1/s), 작은 흔들림까지 줄임
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
            // 위·아래 구간이 이 점을 당기는 힘 (N, 이 점에서 이웃 점 쪽, 크기는 장력), 이번 Step 서브스텝 중 장력이 가장 큰 순간 (짧게 튀는 장력도 잡음)
            public Vector3 upperForce;          // 위쪽(팁 쪽) 구간
            public Vector3 lowerForce;          // 아래쪽(바늘 쪽) 구간
            public float UpperTension => upperForce.magnitude;
            public float LowerTension => lowerForce.magnitude;

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
        public const float WaterDensity = 1000f;        // kg/m³
        private const float WaterViscosity = 0.000001f; // 물 동점성 계수 (m²/s)
        private const float CastHeight = 50f;
        private const float CastDistance = 100f;
        private const float ContactEpsilon = 0.001f;

        [Header("Line Segment")]
        [SerializeField] private float segmentLength = 0.2f;        // 일반 줄 점 간격 (m)
        [SerializeField] private float lineDensity = 0.0001f;       // 줄 1m 의 질량 (kg/m), 줄 점 질량 = 이 값 · 점 간격
        [SerializeField] private float lineDiameter = 0.0003f;      // 줄 굵기 (m), 공기·물 저항과 부력 계산에 씀
        [SerializeField, Min(0f)] private float lineDamping = 0.5f; // 줄 점 속도 비례 감쇠 (1/s), 0 이면 공기·물 저항만
        [SerializeField, Min(0f)] private float lineFloatStiffness = 200f;      // 찌 위쪽 줄 점이 잠긴 깊이 1m 당 떠오르는 가속 (1/s²)
        [SerializeField, Min(0f)] private float lineFloatMaxLift = 20f;         // 떠오르는 가속 최대값 (m/s², 중력 포함), 이 값이 줄 장력과 겨룸
        [SerializeField, Range(0f, 1f)] private float lineGroundFriction = 0.2f;

        [Header("Physics")]
        [SerializeField] private Vector3 gravity = new Vector3(0f, -9.81f, 0f);
        [SerializeField, Min(1)] private int subStepCount = 8;
        // 서브스텝마다 길이 제약을 푸는 최대 횟수 (수렴하면 일찍 끝남)
        // 수면에 일직선으로 놓인 긴 줄은 거의 모든 구간이 정해진 길이 경계에 걸쳐 반복마다 당김/처짐 판정이 바뀌므로, 적으면 남은 오차로 장력이 튀어 찌가 떨림
        [SerializeField, Min(1)] private int iterationCount = 16;
        // 수렴 기준 (m): 당기는 구간들의 남은 오차(늘어남 − 탄성 몫)가 모두 이보다 작고 당기는 구간 판정이 바뀌지 않으면 끝냄
        [SerializeField, Min(0f)] private float solveTolerance = 0.00001f;
        // 장력이 클 때 가벼운 줄 점이 옆으로 튀지 않게, 풀이에서만 질량을 최소 stability · 장력 · Δt² / 점 간격 으로 올림
        // (장력이 작으면 원래 질량 그대로, 클수록 안정적이지만 팽팽한 줄의 옆 흔들림이 조금 느려짐)
        [SerializeField, Min(0f)] private float stability = 2f;
        // 줄 점 개수 상한: 수치가 발산하거나 줄이 비정상적으로 길어져도 점을 끝없이 넣다가 멈추지 않게
        [SerializeField, Min(16)] private int maxSegmentCount = 2000;

        [Header("Contact")]
        [SerializeField] private float groundOffset = 0.02f;
        [SerializeField] private float waterOffset = 0.01f;     // 수면 판정 오차 (m): 접촉 상태(inWater)와 줄 잠김 비율의 경계 폭

        private readonly List<Segment> segments = new List<Segment>();
        private bool hasWaterContact;
        private bool hasWarnedSegmentLimit;
        private Segment releaseSegment;     // null 이 아니면 팁 ~ 이 점 사이가 당기는 만큼 풀림 (캐스팅 중 베일을 연 상태)
        private float releaseAllowance;     // 풀지 않고 남겨 둘 늘어남 (m): 던질 때 손으로 늘어나 있던 양, 탄성으로 줄어든 만큼 같이 줄어듦
        private float releaseAllowanceRate; // 남겨 둘 양을 1초에 줄이는 양 (m/s): keepTime 동안 0 까지 (로드가 펴지는 시간)

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
        private bool[] actives = new bool[16];      // 이번 반복에서 당기는(풀이 대상) 구간인지

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

        // 팁 구간(0) 장력의 마지막 Step 서브스텝 평균 (N)
        public float TipTension => tipTensionAverage;
        private float tipTensionAverage;

        // 로드 팁 스프링: 켜면 팁은 고정점이 아니라, Step 의 tipPosition(앵커: 정적 휨만 반영한 목표 팁)에 스프링으로 묶인 질점
        // 줄 길이 제약과 같은 반복 안에서 같이 풀어서, 줄 장력과 로드 휨이 지연 없이 평형을 찾음
        // 힘(처짐 d) = F0 · −ln(1 − d / maxDistance): 처음엔 무르고 maxDistance 에 가까울수록 단단해짐 (referenceForce 에서 referenceDistance)
        [Serializable]
        public struct TipSpring
        {
            public bool enabled;
            public float frequency;         // 하중 없을 때 팁 고유 진동수 (Hz)
            public float dampingRatio;      // 앵커 기준 감쇠비
            public float airDrag;           // 공기 저항 (1/s)
            public float referenceForce;    // 기준 하중 (N)
            public float referenceDistance; // 기준 하중일 때 처짐 (m)
            public float maxDistance;       // 최대 처짐 (m)
            public Vector3 pivot;           // 로드 그립 위치 (월드): 팁은 그립에서 앵커까지 거리를 반지름으로 하는 구면 위에서만 움직임 (로드는 축 방향으로 줄어들지 않음)
        }

        private TipSpring tipSpring;
        private Vector3 tipAnchorStart;     // 이번 Step 시작 앵커
        private Vector3 tipAnchorEnd;       // 이번 Step 끝 앵커 (Step 의 tipPosition)
        private Vector3 tipAnchor;          // 이번 서브스텝 앵커
        private Vector3 tipPivotStart;
        private Vector3 tipPivot;           // 이번 서브스텝 그립 위치
        private float tipAnchorLambda;
        private float tipSubDeltaTime;
        private bool hasTipAnchor;

        public Vector3 TipVelocity => segments.Count > 0 && tipSubDeltaTime > 0f ? (segments[0].position - segments[0].previous) / tipSubDeltaTime : Vector3.zero;

        float TipSpringF0 => tipSpring.referenceForce / -Mathf.Log(1f - Mathf.Clamp(tipSpring.referenceDistance / tipSpring.maxDistance, 0.001f, TipSpringKneeRatio));

        // 처짐 d 에서 할선 강성 (N/m), d → 0 이면 접선 강성 F0 / maxDistance
        // 0.9 · maxDistance 를 넘으면 그 점의 접선으로 이어서 (힘이 무한대로 가거나 꺾이지 않게) 아주 단단한 선형 스프링
        const float TipSpringKneeRatio = 0.9f;

        float GetTipForce(float distance)
        {
            float dmax = tipSpring.maxDistance;
            float f0 = TipSpringF0;
            float ratio = distance / dmax;
            if (ratio <= TipSpringKneeRatio)
                return f0 * -Mathf.Log(1f - ratio);

            float kneeForce = f0 * -Mathf.Log(1f - TipSpringKneeRatio);
            float kneeSlope = f0 / (dmax * (1f - TipSpringKneeRatio));
            return kneeForce + kneeSlope * (distance - TipSpringKneeRatio * dmax);
        }

        float GetTipStiffness(float distance)
        {
            float dmax = tipSpring.maxDistance;
            if (distance < dmax * 0.001f)
                return TipSpringF0 / dmax;

            return GetTipForce(distance) / distance;
        }

        bool IsTipSpringValid => tipSpring.enabled && tipSpring.maxDistance > 0.001f && tipSpring.referenceDistance > 0f && tipSpring.referenceForce > 0f && tipSpring.frequency > 0f;

        // 팁을 pos 로 옮김 (속도 유지): 로드 본 피팅 결과(메시 팁)에 줄 시작을 맞출 때
        public void SnapTip(Vector3 position)
        {
            if (segments.Count == 0)
                return;

            Segment tip = segments[0];
            Vector3 shift = position - tip.position;
            tip.position += shift;
            tip.previous += shift;
        }

        // 수면에 닿은 일반 줄 점이 수면 위에 얹힘 (물고기를 향해 물속으로 들어가는 줄은 끔)
        public void SetWaterContact(bool isEnabled)
        {
            hasWaterContact = isEnabled;
        }

        // 이번 Step 에서 당겨져 풀린 줄 길이 (SetFreeRelease 가 켜져 있을 때)
        public float ReleasedLength { get; private set; }

        // 팁 ~ segment 사이가 당기는 만큼 풀림 (null 이면 끔)
        // 서브스텝마다 길이 제약을 푼 뒤 늘어난 구간을 실제 거리만큼 늘려서, 장력이 쌓이지 않고 줄이 따라 풀려 나감
        // segment 아래(장비 사이) 길이는 그대로
        // keepTime: 지금 늘어나 있는 양을 이 시간 동안 0 까지 줄여 가며 남겨 둠 (로드가 펴지는 시간)
        //   피칭에서 손으로 늘어난 줄은 그동안 줄 길이가 되지 않고 탄성으로 줄어들며 채비를 당김, 줄어들지 않으면 시간이 지나며 조금씩 풀림
        //   0 이면 남겨 두지 않고 바로 당기는 만큼 풀림
        public void SetFreeRelease(Segment segment, float keepTime = 0f)
        {
            releaseSegment = segment;
            releaseAllowance = segment != null && keepTime > 0f ? GetStretch(segment) : 0f;
            releaseAllowanceRate = keepTime > 0f ? releaseAllowance / keepTime : 0f;
        }

        // 서브스텝마다: 남겨 둘 양보다 더 늘어난 만큼만 풀림
        void ApplyFreeRelease(float deltaTime)
        {
            if (releaseSegment == null)
                return;

            // 수치가 발산해 늘어난 길이가 무한대·NaN 이면 줄 높이에 넣지 않음 (줄 점을 끝없이 채우다 멈추지 않게)
            float stretch = GetStretch(releaseSegment);
            if (float.IsFinite(stretch) == false)
                return;

            releaseAllowance = Mathf.Max(0f, releaseAllowance - releaseAllowanceRate * deltaTime);
            releaseAllowance = Mathf.Min(releaseAllowance, stretch);
            if (stretch > releaseAllowance + 0.000001f)
                ReleasedLength += RelaxStretch(releaseSegment, (stretch - releaseAllowance) / stretch);
        }

        // 팁 ~ segment 구간들의 늘어난 길이 합 (m, 높이는 바꾸지 않음)
        public float GetStretch(Segment segment)
        {
            int index = segments.IndexOf(segment);
            float total = 0f;
            for (int j = 0; j < index; ++j)
                total += GetSpanStretch(j);
            return total;
        }

        // 구간 j 가 정해진 길이보다 늘어난 길이 (m)
        float GetSpanStretch(int span)
        {
            float distance = (segments[span].position - segments[span + 1].position).magnitude;
            return Mathf.Max(0f, distance - GetSpanLength(span));
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
        // - ratio: 늘어난 길이 중 이 비율만큼만 늘림 (1 이면 장력이 다 없어짐)
        public float RelaxStretch(Segment segment, float ratio = 1f)
        {
            int index = segments.IndexOf(segment);
            if (index <= 0)
                return 0f;

            // 높이를 바꾸기 전에 구간마다 늘어난 길이를 먼저 구함 (바꾸면서 구하면 아래 점이 올라간 만큼 위 구간 길이가 줄어 보임)
            relaxStretches.Clear();
            for (int j = 0; j < index; ++j)
            {
                float stretch = GetSpanStretch(j) * ratio;
                relaxStretches.Add(float.IsFinite(stretch) ? stretch : 0f);
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
            tipAnchorEnd = tipAnchor = tipPosition;
            hasTipAnchor = true;
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
            Step(deltaTime, tipPosition, stiffness, default);
        }

        // spring.enabled 면 tipPosition 은 팁 앵커 (정적 휨만 반영한 목표 팁), 아니면 팁 고정 위치
        public void Step(float deltaTime, Vector3 tipPosition, float stiffness, TipSpring spring)
        {
            ReleasedLength = 0f;
            tipPivotStart = tipSpring.enabled && hasTipAnchor ? tipSpring.pivot : spring.pivot;
            tipSpring = spring;
            tipAnchorStart = hasTipAnchor ? tipAnchorEnd : tipPosition;
            tipAnchorEnd = tipPosition;
            hasTipAnchor = true;
            if (IsTipSpringValid == false)
                tipAnchor = tipPosition;

            if (segments.Count < 2 || deltaTime <= 0f)
            {
                // 줄 점이 없으면 팁은 앵커를 그대로 따라감
                if (segments.Count > 0)
                    segments[0].position = segments[0].previous = tipPosition;
                return;
            }

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
                segment.upperForce = Vector3.zero;
                segment.lowerForce = Vector3.zero;
            }

            // 팁 스프링: 팁 질량 = 접선 강성 / ω² (진동수와 기준 휨을 따로 정할 수 있게)
            if (IsTipSpringValid)
            {
                float omega = 2f * Mathf.PI * tipSpring.frequency;
                float tipMass = GetTipStiffness(0f) / (omega * omega);
                segments[0].inverseMass = 1f / Mathf.Max(MinMass, tipMass);
            }
            UpdateContactHeights();

            stiffness = Mathf.Max(0.0001f, stiffness);

            int stepCount = Mathf.Max(1, subStepCount);
            float subDeltaTime = deltaTime / stepCount;
            tipSubDeltaTime = subDeltaTime;
            float tipTensionSum = 0f;
            for (int s = 0; s < stepCount; ++s)
            {
                float t = (s + 1f) / stepCount;
                ApplyFeeds(1f / stepCount);
                Integrate(subDeltaTime, tipPosition, t);

                foreach (var segment in segments)
                    segment.controller?.OnSubStep(segment, subDeltaTime);

                UpdateSolveMasses(subDeltaTime);
                SolveLength(subDeltaTime, stiffness);

                // 당기는 만큼 풀림: 늘어난 구간을 늘려 장력을 없앰 (길어진 구간은 다음 Step 의 NormalizeSegments 가 줄 점으로 채움)
                ApplyFreeRelease(subDeltaTime);

                // 다음 서브스텝 풀이용 질량에 씀
                for (int j = 0; j < SpanCount; ++j)
                {
                    float tension = Mathf.Max(0f, -lambdas[j] / (subDeltaTime * subDeltaTime));
                    tensions[j] = tension;
                    if (tension <= 0f || float.IsFinite(tension) == false)
                        continue;

                    Segment upper = segments[j];
                    Segment lower = segments[j + 1];
                    Vector3 span = lower.position - upper.position;
                    float spanLength = span.magnitude;
                    if (float.IsFinite(spanLength) == false || spanLength < 1e-6f)
                        continue;

                    Vector3 down = span / spanLength;
                    if (tension > lower.UpperTension)
                        lower.upperForce = -down * tension;
                    if (tension > upper.LowerTension)
                        upper.lowerForce = down * tension;
                }

                if (SpanCount > 0)
                    tipTensionSum += tensions[0];
            }

            // 팁 구간 장력의 이번 Step 서브스텝 평균 (순간값보다 덜 튐, 로드 휨에 씀)
            tipTensionAverage = tipTensionSum / stepCount;

            pendingFeeds.Clear();

            UpdateContactStates();
            ApplyGroundFriction();
        }

        // 모든 점을 줄 기본 속성으로: 질량 = 줄 밀도 · 점이 맡은 줄 길이 (위아래 구간의 절반씩), 장비 몸체 없음
        void ResetLineProperties()
        {
            for (int i = 0; i < segments.Count; ++i)
            {
                float upperLength = i > 0 ? GetSpanLength(i - 1) : 0f;
                float lowerLength = i < segments.Count - 1 ? GetSpanLength(i) : 0f;

                Segment segment = segments[i];
                segment.lineLength = (upperLength + lowerLength) * 0.5f;
                segment.mass = lineDensity * segment.lineLength;
                segment.bodyDiameter = 0f;
                segment.bodyDragScale = 1f;
                segment.bodyVolume = 0f;
                segment.bodyHeight = 0f;
                segment.bodyPivot = 0f;
                segment.damping = lineDamping;
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
                while (segments[i].height - segments[i + 1].height > length * 1.5f && CanInsertLine())
                    InsertLine(i + 1, segments[i + 1].height + length);
            }
        }

        // 줄 점 개수 상한 (넘으면 더 넣지 않고 처음 한 번만 경고, 구간은 길어진 채로 남음)
        bool CanInsertLine()
        {
            if (segments.Count < maxSegmentCount)
                return true;

            if (hasWarnedSegmentLimit == false)
            {
                hasWarnedSegmentLimit = true;
                Debug.LogWarning($"[FishingLineBody] 줄 점이 상한({maxSegmentCount})에 닿아 더 넣지 않음 (수치 발산이나 비정상적으로 긴 줄)");
            }
            return false;
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

                if (gap > length * 1.5f && finalGap > length * 1.5f && CanInsertLine())
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
            actives = new bool[size];
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

        // 공기·물 저항 + 감쇠를 "1초에 줄어드는 속도 비율" 로 (암시적으로 v /= 1 + 값 · Δt 에 씀)
        // 저항 힘을 모양으로 구하고 전체 질량으로 나눔 (같은 크기면 무거운 점일수록 덜 감속)
        // - 줄 몫: 가는 원기둥, 힘 = 0.5 · ρ · Cd · 굵기 · 맡은 길이 · v², Cd = 1 + 10 / Re^(2/3)
        //   0.3mm 줄은 Re 가 작아서(공기 1 m/s 에 20) Cd 가 2 ~ 7 로 큼 → 느릴수록 속도에 비례하는 저항에 가까워짐
        //   물은 밀도가 공기의 약 800 배라 물에 잠긴 줄은 1 m/s 에서 약 1800/s (수면에 떠 있으면 절반, 공기 약 4/s)
        // - 몸체 몫(장비): 구, 힘 = 0.5 · ρ · Cd · bodyDragScale · π(d/2)² · v², Cd = 24/Re + 6/(1 + √Re) + 0.4 (느리면 점성 저항, 빠르면 약 0.4 ~ 0.5)
        // lineSubmerged, bodySubmerged (0 ~ 1): 줄 몫, 몸체 몫이 물에 잠긴 비율 (잠긴 만큼 공기 → 물)
        float GetDragRate(Segment segment, float speed, float lineSubmerged, float bodySubmerged)
        {
            float totalMass = Mathf.Max(MinMass, segment.mass);

            float lineForce = GetLineDragForce(segment.lineLength, speed, AirDensity, AirViscosity);
            if (lineSubmerged > 0f)
                lineForce = Mathf.Lerp(lineForce, GetLineDragForce(segment.lineLength, speed, WaterDensity, WaterViscosity), lineSubmerged);

            float bodyForce = 0f;
            if (segment.bodyDiameter > 0f)
            {
                bodyForce = GetBodyDragForce(segment, speed, AirDensity, AirViscosity);
                if (bodySubmerged > 0f)
                    bodyForce = Mathf.Lerp(bodyForce, GetBodyDragForce(segment, speed, WaterDensity, WaterViscosity), bodySubmerged);
            }

            return (lineForce + bodyForce) * speed / totalMass + segment.damping;
        }

        // 가는 원기둥 저항 계수 (힘 = 이 값 · v²), Cd = 1 + 10 / Re^(2/3), Re = v · 굵기 / ν
        float GetLineDragForce(float length, float speed, float density, float viscosity)
        {
            float reynolds = Mathf.Max(0.1f, speed * lineDiameter / viscosity);
            float dragCoefficient = 1f + 10f / Mathf.Pow(reynolds, 2f / 3f);
            return 0.5f * density * dragCoefficient * lineDiameter * length;
        }

        // 장비 몸체(구) 저항 계수 (힘 = 이 값 · v²), Cd = 24/Re + 6/(1 + √Re) + 0.4 (White 근사), Re = v · 지름 / ν
        float GetBodyDragForce(Segment segment, float speed, float density, float viscosity)
        {
            float diameter = segment.bodyDiameter;
            float reynolds = Mathf.Max(0.1f, speed * diameter / viscosity);
            float dragCoefficient = 24f / reynolds + 6f / (1f + Mathf.Sqrt(reynolds)) + 0.4f;
            float area = Mathf.PI * diameter * diameter * 0.25f;
            return 0.5f * density * dragCoefficient * segment.bodyDragScale * area;
        }

        // 물에 잠긴 부피만큼 중력을 줄이는 비율 (물 밀도 · 잠긴 부피 / 전체 질량)
        // 줄 부피 = π(굵기/2)² · 맡은 길이 (줄 밀도 0.0001 kg/m 면 무게의 약 70% 상쇄), 장비 몸체 부피 = bodyVolume (질량 / 재질 밀도)
        // 몸체 부력은 잠긴 깊이에 비례하는 스프링 (세기 = g · 다 잠겼을 때 비율 / bodyHeight)
        // 수치 안정: √세기 · Δt ≤ 1 로 묶음 (넘으면 서브스텝마다 지나쳤다 돌아오며 발산, 3cm 찌·Δt 0.0025s 면 약 490 배까지라 실제 찌 부력 범위엔 영향 없음)
        float GetBuoyancyRatio(Segment segment, float lineSubmerged, float bodySubmerged, float deltaTime)
        {
            float mass = Mathf.Max(MinMass, segment.mass);
            float lineVolume = Mathf.PI * lineDiameter * lineDiameter * 0.25f * segment.lineLength;
            float lineRatio = WaterDensity * lineVolume / mass;

            float bodyRatio = WaterDensity * segment.bodyVolume / mass;
            if (segment.bodyHeight > 0f)
                bodyRatio = Mathf.Min(bodyRatio, segment.bodyHeight / (Mathf.Max(0.01f, -gravity.y) * deltaTime * deltaTime));

            return lineRatio * lineSubmerged + bodyRatio * bodySubmerged;
        }

        // 고정점: 스텝 목표 위치까지 t 비율로
        // 나머지: 속도 + 공기·물 저항·감쇠 + 수면 줄 물 저항 + 중력(잠긴 만큼 부력으로 줄어듦)
        void Integrate(float deltaTime, Vector3 tipPosition, float t)
        {
            float deltaTime2 = deltaTime * deltaTime;
            Vector3 gravityMove = gravity * deltaTime2;
            bool isAboveBobber = true;
            foreach (var segment in segments)
            {
                if (segment.kind == SegmentKind.Bobber)
                    isAboveBobber = false;

                Vector3 velocity = segment.position - segment.previous;
                segment.previous = segment.position;

                if (segment.IsTip)
                {
                    if (IsTipSpringValid)
                    {
                        // 앵커 기준 감쇠 + 공기 저항 (중력은 로드 앵커에 이미 들어 있음)
                        Vector3 previousAnchor = tipAnchor;
                        tipAnchor = Vector3.Lerp(tipAnchorStart, tipAnchorEnd, t);
                        tipPivot = Vector3.Lerp(tipPivotStart, tipSpring.pivot, t);
                        Vector3 anchorMove = tipAnchor - previousAnchor;

                        float omega = 2f * Mathf.PI * tipSpring.frequency;
                        float damping = 2f * tipSpring.dampingRatio * omega;
                        Vector3 relative = (velocity - anchorMove) / (1f + damping * deltaTime);
                        velocity = (anchorMove + relative) / (1f + Mathf.Max(0f, tipSpring.airDrag) * deltaTime);
                        segment.position += velocity;
                    }
                    else
                    {
                        segment.position = Vector3.Lerp(segment.stepStart, tipPosition, t);
                    }
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

                // 잠긴 비율
                // - 줄 몫: 줄 중심이 수면이면 반쯤 잠김 (떠 있는 줄도 물 원기둥 저항을 절반 받아 물에 붙잡힘, 처진 줄이 쉽게 펴지지 않아 로드의 작은 움직임이 찌까지 가지 않음)
                //   waterOffset 만큼 위면 0, 아래면 다 잠김 (줄 굵기로 경계를 잡으면 수면에서 0 과 반 사이를 오가며 떨림)
                // - 몸체 몫: 몸체 높이(bodyHeight) 중 수면 아래 비율, 점 위치는 bodyPivot (구는 중심 0.5: 중심이 수면이면 반, 찌는 아래 끝 0: 몸통 높이만큼 잠기면 다 잠김)
                // 잠긴 만큼 물 저항과 부력 (찌 밑 채비가 공기 중처럼 흔들리지 않고 물에 붙잡혀 천천히 따라감, 착수 시 수면 줄 출렁임도 잡음)
                float waterDepth = segment.waterHeight - segment.position.y;
                float lineSubmerged = Mathf.Clamp01(0.5f + waterDepth / (2f * Mathf.Max(0.001f, waterOffset)));
                float bodySubmerged = segment.bodyHeight > 0f ? Mathf.Clamp01(waterDepth / segment.bodyHeight + segment.bodyPivot) : 0f;

                float speed = velocity.magnitude / deltaTime;
                velocity /= 1f + GetDragRate(segment, speed, lineSubmerged, bodySubmerged) * deltaTime;

                // 부력: 잠긴 부피만큼 중력이 줄어듦 (물보다 가벼운 찌는 잠긴 만큼 떠오름)
                float buoyancyRatio = lineSubmerged > 0f || bodySubmerged > 0f ? GetBuoyancyRatio(segment, lineSubmerged, bodySubmerged, deltaTime) : 0f;
                segment.position += velocity + gravityMove * (1f - buoyancyRatio);

                // 찌 위쪽 줄 점은 잠긴 깊이만큼 수면으로 떠오름 (수면에서 중력과 비김, 최대 lineFloatMaxLift)
                // 서브스텝마다 작은 이동만 주므로, 줄이 당기면 장력이 이겨 물속에 남음 (팁이 물속이어도 줄이 늘어나지 않음)
                float depth = segment.waterHeight - segment.position.y;
                if (hasWaterContact && isAboveBobber && segment.IsLine && depth > 0f)
                {
                    float lift = Mathf.Min(-gravity.y + lineFloatStiffness * depth, lineFloatMaxLift);
                    segment.position.y += lift * deltaTime2;
                }
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
            tipAnchorLambda = 0f;

            for (int iteration = 0; iteration < iterationCount; ++iteration)
            {
                // 로드 팁 스프링 (앵커까지 거리 0 제약, 줄 구간과 번갈아 풂)
                SolveTipAnchor(deltaTime2);

                for (int j = 0; j < spanCount; ++j)
                {
                    Vector3 offset = segments[j].position - segments[j + 1].position;
                    float distance = offset.magnitude;
                    normals[j] = distance > 0.000001f ? offset / distance : Vector3.up;
                }

                bool activeChanged = false;
                float maxError = 0f;
                for (int j = 0; j < spanCount; ++j)
                {
                    Segment a = segments[j], b = segments[j + 1];
                    float weightSum = a.solveInverseMass + b.solveInverseMass;
                    float restLength = GetSpanLength(j);
                    float constraint = (a.position - b.position).magnitude - restLength;

                    // 처져 있고 쌓인 장력도 없는 구간은 풀지 않음 (Δλ = 0)
                    bool active = weightSum > 0f && (constraint > 0f || lambdas[j] < 0f);
                    if (active != actives[j])
                        activeChanged = true;
                    actives[j] = active;

                    if (active == false)
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
                    maxError = Mathf.Max(maxError, Mathf.Abs(rhs[j]));
                }

                // 수렴: 당기는 구간 판정이 그대로이고 남은 오차가 모두 작으면 끝냄 (처진 줄·짧은 줄은 일찍 끝나고, 길고 팽팽한 줄만 많이 돎)
                if (iteration > 0 && activeChanged == false && maxError < solveTolerance)
                    break;

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

        // 팁 → 앵커 거리 0 제약 (XPBD), 비선형 강성은 지금 처짐의 할선 강성으로
        void SolveTipAnchor(float deltaTime2)
        {
            if (IsTipSpringValid == false || segments.Count == 0)
                return;

            Segment tip = segments[0];
            float inverseMass = tip.solveInverseMass > 0f ? tip.solveInverseMass : tip.inverseMass;
            if (inverseMass <= 0f)
                return;

            Vector3 offset = tip.position - tipAnchor;
            float distance = offset.magnitude;
            if (distance < 1e-7f)
                return;

            float compliance = 1f / (GetTipStiffness(distance) * deltaTime2);
            float deltaLambda = (-distance - compliance * tipAnchorLambda) / (inverseMass + compliance);
            tipAnchorLambda += deltaLambda;
            tip.position += offset / distance * (deltaLambda * inverseMass);

            // 로드가 휠 수 있는 범위(최대 처짐) 밖으로는 가지 않음: 넘으면 로드 본이 따라가지 못해 메시 팁과 줄 시작이 어긋나고 줄이 꺾임
            Vector3 deflection = tip.position - tipAnchor;
            float maxDistance = tipSpring.maxDistance;
            if (deflection.sqrMagnitude > maxDistance * maxDistance)
                tip.position = tipAnchor + deflection.normalized * maxDistance;

            // 로드는 축 방향으로 줄어들거나 늘어나지 않음: 그립에서 앵커까지 거리의 구면 위로
            float radius = (tipAnchor - tipPivot).magnitude;
            Vector3 fromPivot = tip.position - tipPivot;
            if (radius > 0.01f && fromPivot.sqrMagnitude > 1e-8f)
                tip.position = tipPivot + fromPivot.normalized * radius;
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

        // 땅 아래로 내려가지 않게 (수면 처리는 Integrate 의 물 저항 + 떠오름)
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
