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
    // - 장비 점(팁, 손, 찌, 봉돌, 바늘) 사이는 segmentLength 간격에 가까운 일반 줄 점으로 채움 (구간 길이가 바뀌면 점 수를 다시 맞춤)
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

            public bool IsLine => kind == SegmentKind.Line;
            public bool IsTip => kind == SegmentKind.Tip;
            public bool IsPinned => IsTip || trackingTarget != null;
            public bool HasWater => float.IsNegativeInfinity(waterHeight) == false;
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

        private List<Segment> segments = new List<Segment>();
        private List<Segment> layoutBuffer = new List<Segment>();
        private readonly List<Segment> sectionLines = new List<Segment>();
        private bool hasWaterContact;

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

        // 팁 높이 = 전체 줄 길이
        public float TotalLength { get; set; }

        // 구간 j (점 j ~ j+1) 의 줄 길이와 장력 (N)
        public float GetSpanLength(int span) => Mathf.Max(MinSpanLength, segments[span].height - segments[span + 1].height);
        public float GetTension(int span) => span >= 0 && span < SpanCount ? tensions[span] : 0f;

        // 수면에 닿은 일반 줄 점이 수면 위에 얹힘 (물고기를 향해 물속으로 들어가는 줄은 끔)
        public void SetWaterContact(bool isEnabled)
        {
            hasWaterContact = isEnabled;
        }

        // 팁 점만 있는 줄로 시작
        public void Build(Vector3 tipPosition, float totalLength)
        {
            segments.Clear();
            TotalLength = totalLength;
            segments.Add(new Segment() { kind = SegmentKind.Tip, height = totalLength, position = tipPosition, previous = tipPosition });
        }

        // 장비 점 추가 (높이 순 위치에 넣음, 사이 줄 점은 다음 스텝에 맞춰짐)
        // 고정점이면 추적 대상 위치, 아니면 위아래 장비 점 사이 높이 비율 위치, 아래가 없으면 위에서 아래로 매달림
        public Segment AddSegment(SegmentKind kind, float height, Transform trackingTarget = null)
        {
            var segment = CreateSegment(kind);
            segment.height = ClampHeight(height);
            segment.trackingTarget = trackingTarget;

            int index = 1;
            while (index < segments.Count && segments[index].height >= segment.height)
                index++;
            segments.Insert(index, segment);

            if (segment.IsPinned)
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
            return segment;
        }

        // 장비 점 제거 (양쪽 줄 점은 다음 스텝에 한 구간으로 다시 맞춰짐)
        public void RemoveSegment(Segment segment)
        {
            if (segment != null && segment.IsTip == false)
                segments.Remove(segment);
        }

        public void SetHeight(Segment segment, float height)
        {
            if (segment != null && segment.IsTip == false)
                segment.height = ClampHeight(height);
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
            return Mathf.Clamp(height, 0f, Mathf.Max(0f, TotalLength - MinSpanLength));
        }

        // stiffness: 줄을 1 배 늘리는 데 드는 힘 (N)
        public void Step(float deltaTime, Vector3 tipPosition, float stiffness)
        {
            if (segments.Count < 2 || deltaTime <= 0f)
                return;

            UpdateLayout();
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
                Integrate(subDeltaTime, tipPosition, t);

                foreach (var segment in segments)
                    segment.controller?.OnSubStep(segment, subDeltaTime);

                UpdateSolveMasses(subDeltaTime);
                SolveLength(subDeltaTime, stiffness);

                // 다음 서브스텝 풀이용 질량에 씀
                for (int j = 0; j < SpanCount; ++j)
                    tensions[j] = Mathf.Max(0f, -lambdas[j] / (subDeltaTime * subDeltaTime));
            }

            UpdateContactStates();
            ApplyGroundFriction();
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

        // 장비 점 사이 구간마다 줄 점 수를 segmentLength 간격에 맞추고, 줄 점 높이를 고르게 나눔
        void UpdateLayout()
        {
            segments[0].height = TotalLength;

            // 장비 점 높이 순서가 바뀌었으면 (예: 찌를 봉돌 아래로 내림) 장비 점만 다시 정렬하고 줄 점은 새로 채움
            bool isSorted = true;
            Segment lastAnchor = null;
            foreach (var segment in segments)
            {
                if (segment.IsLine)
                    continue;

                if (segment.IsTip == false)
                    segment.height = ClampHeight(segment.height);
                if (lastAnchor != null && segment.height > lastAnchor.height)
                    isSorted = false;
                lastAnchor = segment;
            }

            if (isSorted == false)
            {
                segments.RemoveAll(x => x.IsLine);
                segments.Sort(1, segments.Count - 1, Comparer<Segment>.Create((a, b) => b.height.CompareTo(a.height)));
            }

            layoutBuffer.Clear();
            int index = 0;
            while (index < segments.Count)
            {
                Segment above = segments[index];
                layoutBuffer.Add(above);

                // 다음 장비 점까지의 줄 점
                sectionLines.Clear();
                int next = index + 1;
                while (next < segments.Count && segments[next].IsLine)
                    sectionLines.Add(segments[next++]);

                // 마지막 장비 점 아래로 남은 줄 점은 버림
                if (next >= segments.Count)
                    break;

                Segment below = segments[next];
                float length = Mathf.Max(MinSpanLength, above.height - below.height);
                int count = Mathf.Max(0, Mathf.RoundToInt(length / Mathf.Max(0.01f, segmentLength)) - 1);
                if (sectionLines.Count != count)
                    Resample(above, below, count);

                float spacing = length / (count + 1);
                for (int k = 0; k < sectionLines.Count; ++k)
                {
                    sectionLines[k].height = above.height - spacing * (k + 1);
                    layoutBuffer.Add(sectionLines[k]);
                }

                index = next;
            }

            (segments, layoutBuffer) = (layoutBuffer, segments);
        }

        // above ~ below 사이 줄 점을 count 개로: 지금 줄 모양을 따라 길이 비율로 다시 놓음
        void Resample(Segment above, Segment below, int count)
        {
            int oldCount = sectionLines.Count + 2;
            var positions = new Vector3[oldCount];
            var previous = new Vector3[oldCount];
            var cumulative = new float[oldCount];

            positions[0] = above.position;
            previous[0] = above.previous;
            for (int k = 0; k < sectionLines.Count; ++k)
            {
                positions[k + 1] = sectionLines[k].position;
                previous[k + 1] = sectionLines[k].previous;
            }
            positions[oldCount - 1] = below.position;
            previous[oldCount - 1] = below.previous;

            for (int k = 1; k < oldCount; ++k)
                cumulative[k] = cumulative[k - 1] + (positions[k] - positions[k - 1]).magnitude;
            float total = Mathf.Max(0.000001f, cumulative[oldCount - 1]);

            while (sectionLines.Count > count)
                sectionLines.RemoveAt(sectionLines.Count - 1);
            while (sectionLines.Count < count)
                sectionLines.Add(CreateSegment(SegmentKind.Line));

            int cursor = 0;
            for (int k = 0; k < count; ++k)
            {
                float target = total * (k + 1) / (count + 1);
                while (cursor < oldCount - 2 && cumulative[cursor + 1] < target)
                    cursor++;

                float range = Mathf.Max(0.000001f, cumulative[cursor + 1] - cumulative[cursor]);
                float t = Mathf.Clamp01((target - cumulative[cursor]) / range);
                sectionLines[k].position = Vector3.Lerp(positions[cursor], positions[cursor + 1], t);
                sectionLines[k].previous = Vector3.Lerp(previous[cursor], previous[cursor + 1], t);
            }
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
                    segment.position = Vector3.Lerp(segment.stepStart, segment.trackingTarget.position, t);
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
