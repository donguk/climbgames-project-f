using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.ProjectF
{
    // 낚싯줄: FishingLineBody 로 줄 물리를 풀고, 장비 점 위치를 FishingLinePoint 에 넘김
    // - 장비 점: 팁(로드 끝), 찌, 봉돌, 바늘 (+ 피칭으로 잡고 있으면 손), 찌·봉돌·바늘은 처음에 한 번 만들고 계속 씀
    // - 상황에 따라 바늘 점의 추적 대상(trackingTarget)만 바뀜 (던질 때: 날아가는 바늘, 입질: 물고기, 회수: 로드 가이드)
    // - 장비 속성(질량, 저항, 부력)은 장비(FishingBobber, FishingSinker, FishingHook)가 자기 점에 설정함
    public class FishingLine : FishingEquipment
    {
        [SerializeField] private FishingLineBody lineBody = new FishingLineBody();
        [SerializeField] private FishingLinePoint bobberPoint;
        [SerializeField] private FishingLinePoint sinkerPoint;
        [SerializeField] private FishingLinePoint endPoint;
        [SerializeField] private FishingLinePoint handlePoint;
        [SerializeField] private float totalLength;
        [SerializeField] private bool showDebug;

        [Header("Stretch")]
        [SerializeField] private float stiffness = 150f;            // 줄을 1 배 늘리는 데 드는 힘 (N), 높을수록 덜 늘어나고 빠르게 떨림

        [Header("Over Head")]
        // 오버헤드 대기: 팁–찌 줄 길이, 찌 높이(찌–바늘 길이)를 바꾸면 찌 아래에서 줄이 늘고 줆 (팁–찌는 그대로)
        [SerializeField] private float overheadTipLength = 0.4f;

        [Header("Pitching")]
        // 피칭 장비 높이 기준 길이 (m): 손·찌·봉돌을 이 길이의 비율 위치에 둠 (전체 줄 길이는 pitchingHoldTension 으로 정함)
        [SerializeField] private float pitchingLineLength = 1f;
        // 장비를 설치할 수 있는 최대 길이 (m): 피칭에서는 팁·바늘을 뺀 장비(찌, 봉돌, 손)를 팁–바늘 사이 height / 이 값 비율 위치에 둠
        [SerializeField] private float maxAttachDistance = 2f;
        // 장비가 목표 높이로 줄을 따라 미끄러지는 최대 속도 (m/s): 피칭 ↔ 던지기에서 높이 기준이 바뀔 때 순간이동하지 않게
        [SerializeField] private float equipmentSlideSpeed = 2f;
        // 손으로 줄을 잡을 때 팁 장력 목표 (N): 오버헤드 → 피칭 전환 동안 팁–손 직선 거리에서 이 장력이 나오는 길이로 줄을 맞추고, 전환이 끝나면 그 길이를 유지
        [SerializeField, Min(0f)] private float pitchingHoldTension = 1f;

        // 피칭 ↔ 오버헤드 전환 시간 (초): 손이 줄을 잡으러 가는 시간, 줄 길이가 바뀌는 시간
        [SerializeField] private float castingTypeTransitionTime = 0.5f;

        // 찌 착수 뒤에도 줄이 당기는 만큼 계속 풀리는 시간 (초): 공중에 떠 있던 줄이 수면에 내려앉으며 찌를 끌어당기지 않게
        [SerializeField] private float landingReleaseTime = 1f;
        // 던진 뒤 로드가 펴지는 시간 (초): 이 시간 동안 손으로 늘어나 있던 줄이 바로 줄 길이가 되지 않고 채비를 앞으로 당김
        [SerializeField] private float rodRecoverTime = 0.2f;

        private bool isOverhead;
        private float releaseTimer = -1f;   // 착수 뒤 풀림이 남은 시간 (음수면 타이머 없음)
        private bool isPitching;
        private float pitchingTransitionTimer;  // 손이 줄을 잡으러 가는 남은 시간: 이 동안 팁–손 줄 길이를 목표 장력 길이로 맞춤

        private readonly List<Vector3> drawPoints = new List<Vector3>();
        private Vector3[] drawBuffer = new Vector3[64];

        private FishingLineDrawer[] lineDrawers;
        private FishingPlayer player;
        private Transform rodTip;
        private IRodTip rodTipSpring;
        private Transform hook;
        private float lengthSpeed = float.PositiveInfinity;   // 줄 길이를 목표(totalLength)로 맞추는 속도 (m/s), SetLength 의 duration 으로 정함

        public float TotalLength => totalLength;
        public float Stiffness => stiffness;
        public FishingLineBody Body => lineBody;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint SinkerPoint => sinkerPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLinePoint HandlePoint => handlePoint;

        // 로드 팁 스프링을 줄 물리에 넣음 (null 이면 팁은 rodTip 위치 고정점)
        public void SetRodTip(IRodTip rodTipSpring)
        {
            this.rodTipSpring = rodTipSpring;
        }

        public void Initialize(FishingPlayer player, Transform rodTip, Transform hook, float length)
        {
            this.player = player;
            this.rodTip = rodTip;
            this.hook = hook;
            lineDrawers = GetComponentsInChildren<FishingLineDrawer>();

            SetLength(Mathf.Max(1f, length));

            bobberPoint.Initialize();
            sinkerPoint.Initialize();
            endPoint.Initialize();
            handlePoint.Initialize();

            // 팁에 찌·봉돌·바늘이 매달린 줄 (Idle 진입 때 캐스팅 타입에 맞춰 손 점을 붙임)
            lineBody.Build(rodTip.position, totalLength);
            bobberPoint.BindSegment(lineBody.AddSegment(SegmentKind.Bobber, bobberPoint.Height));
            sinkerPoint.BindSegment(lineBody.AddSegment(SegmentKind.Sinker, sinkerPoint.Height));
            endPoint.BindSegment(lineBody.AddSegment(SegmentKind.Hook, endPoint.Height));

            // 장비를 달기 전에 점의 위치와 줄 방향을 맞춤 (조인트는 붙는 순간의 상대 회전을 기준으로 삼음)
            SyncPose(bobberPoint);
            SyncPose(sinkerPoint);
            SyncPose(endPoint);
        }

        void Update()
        {
            if (lineDrawers == null)
                return;

            int drawerIndex = DrawLine();

            // 사용안하는 라인 끄기
            for (int i = drawerIndex; i < lineDrawers.Length; ++i)
                lineDrawers[i].gameObject.SetActive(false);


            // 테스트
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.pageUpKey.isPressed)
                    {
                        if (Keyboard.current.ctrlKey.isPressed)
                            bobberPoint.SetHeight(bobberPoint.Height + 0.005f);
                        else
                            Reeling(0.005f);
                    }

                    if (Keyboard.current.pageDownKey.isPressed)
                    {
                        if (Keyboard.current.ctrlKey.isPressed)
                            bobberPoint.SetHeight(bobberPoint.Height - 0.005f);
                        else
                            Release(0.005f);
                    }
                }
            }
        }

        // FishingActorMachine.OnFixedUpdate 에서 호출
        public void UpdateLine(float deltaTime)
        {
            // 착수 뒤 풀림 시간이 끝나면 줄 풀림을 멈춤
            if (releaseTimer >= 0f)
            {
                releaseTimer -= deltaTime;
                if (releaseTimer <= 0f)
                    StopFreeRelease();
            }

            // 오버헤드: 찌 높이가 바뀌면 찌 아래에서 줄을 늘리고 줄임 (팁–찌 길이 그대로, 전체 줄 길이도 같이)
            // 한 프레임 양은 equipmentSlideSpeed 로 제한 (피칭에서 바뀔 때 높이 기준 차이를 한 번에 넣으면 채비가 툭 떨어짐)
            if (isOverhead && bobberPoint.Segment != null)
            {
                float current = bobberPoint.Segment.height;
                float amount = Mathf.MoveTowards(current, bobberPoint.Height, equipmentSlideSpeed * deltaTime) - current;
                if (Mathf.Abs(amount) > 0.000001f)
                {
                    lineBody.Feed(bobberPoint.Segment, amount);
                    totalLength += amount;
                    player.Model.SetLineLength(totalLength);
                }
            }

            // 오버헤드 → 피칭 전환: 손이 줄을 잡으러 가는 동안, 팁–손 직선 거리에서 목표 장력이 나오는 길이로 줄을 맞춤
            // 장력 = stiffness · (거리 − 줄 길이) / 줄 길이 → 줄 길이 = 거리 / (1 + 장력 / stiffness)
            // - 거리는 앵커(정적 휨만 반영한 목표 팁) ~ 지금 손 점: 휜 팁으로 재면 휠수록 짧게 재어져 줄이 짧아지고 다시 더 휘는 되먹임이 생김
            // - 손 점이 멀어지면 바로 그만큼 풀고 (늦으면 장력이 튐), 가까워지면 남은 시간에 나눠 감음
            // - 전환이 끝나면 그 길이를 그대로 유지 (이후 장력 변화는 로드 휨으로만 나타남)
            if (isPitching && pitchingTransitionTimer > 0f && handlePoint.Segment != null)
            {
                Vector3 tipAnchor = rodTipSpring != null ? rodTipSpring.GetTipAnchor() : lineBody.Tip.position;
                float straight = Vector3.Distance(tipAnchor, handlePoint.Segment.position);
                float holdLength = straight / (1f + pitchingHoldTension / Mathf.Max(0.0001f, stiffness));
                float wanted = Mathf.Max(GetMinLength(), handlePoint.Segment.height + holdLength);

                if (wanted > totalLength)
                    totalLength = wanted;
                else
                    totalLength = Mathf.MoveTowards(totalLength, wanted, (totalLength - wanted) / Mathf.Max(pitchingTransitionTimer, deltaTime) * deltaTime);

                lengthSpeed = float.PositiveInfinity;
                player.Model.SetLineLength(totalLength);

                pitchingTransitionTimer -= deltaTime;
            }

            // 팁에서 줄을 늘리고 줄여 전체 줄 길이를 목표(totalLength)로 맞춤 (lengthSpeed 로 일정하게, 가장 높은 장비 점보다 5cm 이상 길게)
            // lineBody.Length 는 위의 찌 Feed 까지 반영한 길이
            float length = Mathf.MoveTowards(lineBody.Length, totalLength, lengthSpeed * deltaTime);
            if (length == totalLength)
                lengthSpeed = float.PositiveInfinity;   // 다 맞추면 이후 감기·풀기는 바로 적용

            length = Mathf.Max(length, GetMinLength());
            lineBody.Feed(lineBody.Tip, length - lineBody.Length);

            // FishingLinePoint 높이 → 줄 점 높이 (오버헤드의 찌는 위에서 Feed 로 처리)
            if (isOverhead == false)
                SyncHeight(bobberPoint, deltaTime);
            SyncHeight(sinkerPoint, deltaTime);
            SyncHeight(handlePoint, deltaTime);
            SyncHeight(endPoint, deltaTime);

            if (rodTipSpring != null)
            {
                // 팁은 앵커에 스프링으로 묶인 질점: 줄 장력과 같이 풀고, 그 위치로 로드 본을 휜 뒤 메시 팁에 줄 시작을 맞춤
                // 로드 끝은 줄 방향을 넘어서 휘지 않음: 지난 스텝 줄 방향으로 휨 한계를 정함
                // (Step 의 팁 궤적 제한과 ApplyTip 본 휨이 같은 한계를 써야 시뮬 팁과 메시 팁이 같은 자리)
                rodTipSpring.SetLineDirection(GetTipLineDirection());
                lineBody.Step(deltaTime, rodTipSpring.GetTipAnchor(), stiffness, rodTipSpring.GetTipSpring());
                lineBody.SnapTip(rodTipSpring.ApplyTip(lineBody.Tip.position));
            }
            else
            {
                lineBody.Step(deltaTime, rodTip.position, stiffness);
            }

            // 캐스팅 중 당겨져 풀린 만큼 목표 길이에 더함 (다음 프레임에 팁 Feed 가 다시 감지 않게)
            if (lineBody.ReleasedLength > 0f)
            {
                totalLength += lineBody.ReleasedLength;
                player.Model.SetLineLength(totalLength);
            }

            // 줄 점 위치 → FishingLinePoint (찌/바늘/물고기가 따라옴)
            SyncPose(bobberPoint);
            SyncPose(sinkerPoint);
            SyncPose(handlePoint);
            SyncPose(endPoint);
        }

        // 팁에서 줄이 나가는 방향: 팁에서 TipDirectionReach 이상 떨어진 첫 줄 점까지 (팁 구간이 아주 짧을 때 방향이 프레임마다 튀지 않게)
        // 점이 2개 미만이거나 모두 겹쳐 있으면 zero
        const float TipDirectionReach = 0.1f;

        Vector3 GetTipLineDirection()
        {
            var segments = lineBody.Segments;
            if (segments.Count < 2)
                return Vector3.zero;

            Vector3 tip = segments[0].position;
            Vector3 direction = Vector3.zero;
            for (int i = 1; i < segments.Count; ++i)
            {
                direction = segments[i].position - tip;
                if (direction.sqrMagnitude >= TipDirectionReach * TipDirectionReach)
                    break;
            }
            return direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.zero;
        }

        // 피칭: 손 점을 붙이고 팁–손 구간 길이를 맞춤
        void AttachHandle(Transform leftHandle)
        {
            if (handlePoint.Segment != null)
                return;

            // 손 점은 줄 위의 그 높이에서 시작해 손 위치로 옮겨가며 줄을 잡음, 줄 길이도 같은 시간 동안 맞춤
            // 전체 줄 길이는 지금(오버헤드) 길이에서 출발해 전환 동안 UpdateLine 이 목표 장력 길이로 맞춤
            pitchingTransitionTimer = Mathf.Max(Time.fixedDeltaTime, castingTypeTransitionTime);
            handlePoint.BindSegment(lineBody.AddSegment(SegmentKind.Handle, GetPointHeight(handlePoint), leftHandle, castingTypeTransitionTime));
        }

        // 손을 놓음: 채비는 팁에 매달림
        // 손으로 잡은 팁–손 줄은 팽팽하게 늘어나 있으므로, 놓기 전에 늘어난 구간마다 그 구간을 늘려 장력을 없앰 (놓는 순간 고무줄처럼 튕기지 않게)
        // 오버헤드 전환은 뒤의 SetLength 가 목표를 다시 정하고, 풀린 길이에서 천천히 감음
        // relaxStretch false (던질 때): 늘어난 양을 줄 길이로 만들지 않음, 늘어난 줄은 탄성으로 줄어들며 채비를 앞으로 당김 (로드 휨 반동처럼)
        void DetachHandle(bool relaxStretch = true)
        {
            if (handlePoint.Segment == null)
                return;

            float stretch = relaxStretch ? lineBody.RelaxStretch(handlePoint.Segment) : 0f;
            if (stretch > 0f)
            {
                totalLength += stretch;
                player.Model.SetLineLength(totalLength);
            }

            lineBody.RemoveSegment(handlePoint.Segment);
            handlePoint.BindSegment(null);
        }

        // 줄 풀림을 멈춤 (착수 뒤 타이머도 정리)
        void StopFreeRelease()
        {
            lineBody.SetFreeRelease(null);
            releaseTimer = -1f;
        }

        // 팁 바로 아래의 장비 점 (찌가 있으면 찌): 캐스팅 중 팁 ~ 이 점 사이가 당기는 만큼 풀림
        FishingLineBody.Segment GetTopEquipmentSegment()
        {
            FishingLineBody.Segment top = null;
            foreach (var point in new[] { bobberPoint, sinkerPoint, endPoint })
            {
                if (point.Segment != null && (top == null || point.Segment.height > top.height))
                    top = point.Segment;
            }
            return top;
        }

        // 줄이 버틸 최소 길이: 가장 높은 장비 점보다 5cm 위
        float GetMinLength()
        {
            float height = 0f;
            foreach (var point in new[] { bobberPoint, sinkerPoint, endPoint, handlePoint })
            {
                if (point.Segment != null)
                    height = Mathf.Max(height, GetPointHeight(point));
            }
            return height + 0.05f;
        }

        // 장비 점 높이: 피칭이면 팁·바늘을 뺀 장비는 팁–바늘 사이 height / maxAttachDistance 비율 위치, 아니면 height 그대로
        float GetPointHeight(FishingLinePoint point)
        {
            if (isPitching && point != endPoint)
                return pitchingLineLength * Mathf.Clamp01(point.Height / Mathf.Max(0.01f, maxAttachDistance));
            return point.Height;
        }

        // 줄 점 높이를 목표 높이로 (바늘 제외 장비는 equipmentSlideSpeed 로 줄을 따라 미끄러짐)
        void SyncHeight(FishingLinePoint point, float deltaTime)
        {
            var segment = point.Segment;
            if (segment == null)
                return;

            float height = GetPointHeight(point);
            if (point != endPoint)
                height = Mathf.MoveTowards(segment.height, height, equipmentSlideSpeed * deltaTime);

            lineBody.SetHeight(segment, height);
        }

        void SyncPose(FishingLinePoint point)
        {
            var segment = point.Segment;
            if (segment != null)
                point.SetPose(segment.position, lineBody.GetUpDirection(segment));
        }

        // 고정점(팁, 손, 추적 중인 바늘)에서 나눠 그리기
        int DrawLine()
        {
            int drawerIndex = 0, start = 0;
            var segments = lineBody.Segments;
            for (int i = 1; i < segments.Count; ++i)
            {
                if (segments[i].IsPinned == false && i < segments.Count - 1)
                    continue;

                drawPoints.Clear();
                lineBody.CopyPositions(drawPoints, start, i);

                if (drawerIndex < lineDrawers.Length && drawPoints.Count > 1)
                {
                    if (drawBuffer.Length < drawPoints.Count)
                        drawBuffer = new Vector3[drawPoints.Count + 16];
                    drawPoints.CopyTo(drawBuffer);

                    lineDrawers[drawerIndex].gameObject.SetActive(true);
                    lineDrawers[drawerIndex].DrawLine(drawBuffer, 0, drawPoints.Count);
                }

                start = i;
                drawerIndex++;
            }
            return drawerIndex;
        }

#if UNITY_EDITOR
        // 전체 줄 길이, 점 수, 팁 쪽 구간 장력
        void OnGUI()
        {
            if (showDebug == false)
                return;

            GUI.Label(new Rect(10f, 10f, 400f, 60f),
                $"Line: {lineBody.Length:F2} m, Segments: {lineBody.Segments.Count}\nTip Tension: {lineBody.GetTension(0):F3} N");
        }
#endif

        // 팁에서 target 까지 직선 거리와, 팁부터 첫 고정점(또는 찌)까지 줄 길이 (파이팅 텐션 계산)
        public LineLengthInfo GetLengthInfo(Transform target)
        {
            float straightLength = Vector3.Distance(rodTip.position, target.position);
            float lineLength = lineBody.Length;
            Vector3 lineDirection = Vector3.zero;

            var segments = lineBody.Segments;
            for (int i = 1; i < segments.Count; ++i)
            {
                if (segments[i].IsPinned || segments[i].kind == SegmentKind.Bobber)
                {
                    lineLength = lineBody.Length - segments[i].height;
                    lineDirection = (rodTip.position - segments[i].position).normalized;
                    break;
                }
            }

            return new LineLengthInfo()
            {
                straightLength = straightLength,
                lineLength = lineLength,
                direction = lineDirection,
            };
        }

        // 목표 줄 길이를 정함: duration 동안 지금 길이에서 일정한 속도로 맞춰짐 (0 이면 바로)
        public void SetLength(float length, float duration = 0f)
        {
            totalLength = length;
            lengthSpeed = duration > 0f ? Mathf.Abs(length - lineBody.Length) / duration : float.PositiveInfinity;

            // 데이터 설정
            player.Model.SetLineLength(totalLength);
        }

        public float Release(float amount)
        {
            if (amount > 0f)
                totalLength += amount;

            player.Model.SetLineLength(totalLength);
            return totalLength;
        }

        public float Reeling(float amount)
        {
            // 가장 높은 장비 점(찌 또는 손)까지는 감지 않음
            if (amount > 0f)
                totalLength = Mathf.Max(GetMinLength(), totalLength - amount);

            player.Model.SetLineLength(totalLength);
            return totalLength;
        }



        // 이벤트 처리

        // Idle 진입 때 (처음, 회수 후 복귀 포함): 채비를 손에 든 상태로 되돌림
        protected override void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
            endPoint.SetTrackingTarget(null);
            lineBody.SetWaterContact(false);
            StopFreeRelease();

            Transform leftHandle = param.type == CastingType.Pitching ? param.leftHandle : null;
            isOverhead = param.type == CastingType.OverHead;
            isPitching = param.type == CastingType.Pitching;
            if (leftHandle != null)
            {
                AttachHandle(leftHandle);
            }
            else
            {
                DetachHandle();

                // 오버헤드: 팁–찌를 overheadTipLength 로
                // 찌가 아직 피칭 비율 위치에 있을 수 있으므로 지금 찌 점 높이 기준 (실제 height 와의 차이는 UpdateLine 의 Feed 가 찌 아래에서 늘리며 줄 길이에도 더함)
                float bobberHeight = bobberPoint.Segment != null ? bobberPoint.Segment.height : bobberPoint.Height;
                SetLength(isOverhead ? bobberHeight + overheadTipLength : totalLength, castingTypeTransitionTime);
            }
        }

        // 손을 놓음 (바늘은 줄에서 떨어져 날아가고 바늘 점이 그 바늘을 추적), 이후 줄 길이는 팁에서만
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            endPoint.SetTrackingTarget(hook);
            DetachHandle(false);
            isOverhead = false;
            isPitching = false;

            // 찌가 착수할 때까지 줄이 당기는 만큼 풀림 (베일을 연 상태)
            // 손으로 늘어나 있던 양은 로드가 펴지는 동안 줄여 가며 남겨 두고, 그보다 더 당겨질 때만 풀림
            lineBody.SetFreeRelease(GetTopEquipmentSegment(), rodRecoverTime);
        }

        // 찌 착수 (찌 점이 수면에 닿아 inWater 가 켜지면 FishingBobber 가 보냄): 줄이 수면에 얹힘
        protected override void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
            // 바늘이 다시 줄 끝에 매달리므로 바늘 점 추적을 끊음
            endPoint.SetTrackingTarget(null);
            lineBody.SetWaterContact(true);

            // 공중에 있던 줄이 내려앉을 때까지 조금 더 풀림
            releaseTimer = landingReleaseTime;
            if (releaseTimer <= 0f)
                StopFreeRelease();
        }

        // (찌 없는 경우) 바늘이 땅에 닿음: 줄 풀림 멈춤
        protected override void OnHookGrounded(FishingEvent.OnHookGrounded param)
        {
            StopFreeRelease();
        }

        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            // 물고기 추적
            endPoint.SetTrackingTarget(param.hookedFish.transform);
            StopFreeRelease();

            // 물고기를 향해 물속으로 들어가는 줄은 수면에 걸리지 않음
            lineBody.SetWaterContact(false);
        }

        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            // 추적 끊기
            endPoint.SetTrackingTarget(null);
            lineBody.SetWaterContact(true);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            lineBody.SetWaterContact(false);
            StopFreeRelease();
            SetLength(1.6f, 1f);

            // 물고기가 잡혔다면 연결
            if (param.hookedFish != null)
            {
                param.hookedFish.AttachToLine(endPoint);
                endPoint.SetTrackingTarget(null);
            }
        }
    }
}
