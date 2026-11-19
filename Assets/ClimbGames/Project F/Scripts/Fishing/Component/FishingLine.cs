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
        [SerializeField] private float pitchingLineLength = 1f;     // 피칭으로 잡고 있을 때 전체 줄 길이 (m)
        // 장비를 설치할 수 있는 최대 길이 (m): 피칭에서는 팁·바늘을 뺀 장비(찌, 봉돌, 손)를 팁–바늘 사이 height / 이 값 비율 위치에 둠
        [SerializeField] private float maxAttachDistance = 2f;
        // 장비가 목표 높이로 줄을 따라 미끄러지는 최대 속도 (m/s): 피칭 ↔ 던지기에서 높이 기준이 바뀔 때 순간이동하지 않게
        [SerializeField] private float equipmentSlideSpeed = 2f;

        // 피칭 ↔ 오버헤드 전환 시간 (초): 손이 줄을 잡으러 가는 시간, 줄 길이가 바뀌는 시간
        [SerializeField] private float castingTypeTransitionTime = 0.5f;

        private bool isOverhead;
        private bool isPitching;

        private readonly List<Vector3> drawPoints = new List<Vector3>();
        private Vector3[] drawBuffer = new Vector3[64];

        private FishingLineDrawer[] lineDrawers;
        private FishingPlayer player;
        private Transform rodTip;
        private float lengthSpeed = float.PositiveInfinity;   // 줄 길이를 목표(totalLength)로 맞추는 속도 (m/s), SetLength 의 duration 으로 정함

        public float TotalLength => totalLength;
        public FishingLineBody Body => lineBody;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint SinkerPoint => sinkerPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLinePoint HandlePoint => handlePoint;

        public void Initialize(FishingPlayer player, Transform rodTip, float length)
        {
            this.player = player;
            this.rodTip = rodTip;
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

            lineBody.Step(deltaTime, rodTip.position, stiffness);

            // 줄 점 위치 → FishingLinePoint (찌/바늘/물고기가 따라옴)
            SyncPose(bobberPoint);
            SyncPose(sinkerPoint);
            SyncPose(handlePoint);
            SyncPose(endPoint);
        }

        // 피칭: 손 점을 붙이고 팁–손 구간 길이를 맞춤
        void AttachHandle(Transform leftHandle)
        {
            if (handlePoint.Segment != null)
                return;

            // 손 점은 줄 위의 그 높이에서 시작해 손 위치로 옮겨가며 줄을 잡음, 줄 길이도 같은 시간 동안 맞춤
            SetLength(pitchingLineLength, castingTypeTransitionTime);
            handlePoint.BindSegment(lineBody.AddSegment(SegmentKind.Handle, GetPointHeight(handlePoint), leftHandle, castingTypeTransitionTime));
        }

        // 손을 놓음: 채비는 팁에 매달림
        // 손으로 잡은 팁–손 줄은 팽팽하게 늘어나 있으므로, 놓기 전에 늘어난 구간마다 그 구간을 늘려 장력을 없앰 (놓는 순간 고무줄처럼 튕기지 않게)
        // 목표 길이(totalLength) 갱신은 던질 때를 위한 것 (오버헤드 전환은 뒤의 SetLength 가 목표를 다시 정하고, 풀린 길이에서 천천히 감음)
        void DetachHandle()
        {
            if (handlePoint.Segment == null)
                return;

            float stretch = lineBody.RelaxStretch(handlePoint.Segment);
            if (stretch > 0f)
            {
                totalLength += stretch;
                player.Model.SetLineLength(totalLength);
            }

            lineBody.RemoveSegment(handlePoint.Segment);
            handlePoint.BindSegment(null);
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

        // 손을 놓음 (바늘 점은 FishingHook 이 Detach 하면서 날아가는 바늘을 추적), 이후 줄 길이는 팁에서만
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            DetachHandle();
            isOverhead = false;
            isPitching = false;
        }

        // 찌 착수 (찌 점이 수면에 닿아 inWater 가 켜지면 FishingBobber 가 보냄): 줄이 수면에 얹힘
        protected override void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
            lineBody.SetWaterContact(true);
        }

        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            // 물고기 추적
            endPoint.SetTrackingTarget(param.hookedFish.ModelRoot);

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
            SetLength(1.6f, 1f);

            // 물고기가 잡혔다면 연결
            if (param.hookedFish != null)
            {
                // 포지션 리셋
                var hookedFish = param.hookedFish;
                hookedFish.transform.position = hookedFish.ModelRoot.position;
                hookedFish.ModelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                endPoint.Attach(param.hookedFish);
            }

            // 바늘을 로드 가이드로 (다음 Idle 진입 때 OnCastingTypeChanged 에서 놓음)
            endPoint.SetTrackingTarget(param.rod.LineGuide);
        }
    }
}
