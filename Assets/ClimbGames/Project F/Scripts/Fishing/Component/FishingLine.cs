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

        private readonly List<Vector3> drawPoints = new List<Vector3>();
        private Vector3[] drawBuffer = new Vector3[64];

        private FishingLineDrawer[] lineDrawers;
        private FishingPlayer player;
        private Transform rodTip;
        public float targetLength;
        private NormalizedTimer targetLengthTimer = new NormalizedTimer(1f);

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
        }

        void Update()
        {
            int drawerIndex = DrawLine();

            // 사용안하는 라인 끄기
            for (int i = drawerIndex; i < lineDrawers.Length; ++i)
                lineDrawers[i].gameObject.SetActive(false);


            // 테스트
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.pageUpKey.isPressed)
                        bobberPoint.SetHeight(bobberPoint.Height + 0.005f);

                    if (Keyboard.current.pageDownKey.isPressed)
                        bobberPoint.SetHeight(bobberPoint.Height - 0.005f);
                }
            }
        }

        // FishingActorMachine.OnFixedUpdate 에서 호출
        public void UpdateLine(float deltaTime)
        {
            // 전체 줄 길이 = 팁 높이 (SetLength 의 duration 만큼 보간, 가장 높은 장비 점보다 5cm 이상 길게)
            targetLengthTimer.Update(deltaTime);
            float length = Mathf.Lerp(lineBody.TotalLength, targetLength, targetLengthTimer.Value);
            lineBody.TotalLength = Mathf.Max(length, GetMinLength());

            // FishingLinePoint 높이 → 줄 점 높이
            SyncHeight(bobberPoint);
            SyncHeight(sinkerPoint);
            SyncHeight(handlePoint);
            SyncHeight(endPoint);

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

            // 팁–손 구간 줄 길이 = 실제 거리 · (1 + slack) 이 원래 식, 지금은 테스트용 고정 길이
            SetLength(1f);

            lineBody.TotalLength = totalLength;
            handlePoint.BindSegment(lineBody.AddSegment(SegmentKind.Handle, handlePoint.Height, leftHandle));
        }

        // 손을 놓음: 채비는 팁에 매달림
        void DetachHandle()
        {
            if (handlePoint.Segment == null)
                return;

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
                    height = Mathf.Max(height, point.Height);
            }
            return height + 0.05f;
        }

        void SyncHeight(FishingLinePoint point)
        {
            if (point.Segment != null)
                lineBody.SetHeight(point.Segment, point.Height);
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
                $"Line: {lineBody.TotalLength:F2} m, Segments: {lineBody.Segments.Count}\nTip Tension: {lineBody.GetTension(0):F3} N");
        }
#endif

        // 팁에서 target 까지 직선 거리와, 팁부터 첫 고정점(또는 찌)까지 줄 길이 (파이팅 텐션 계산)
        public LineLengthInfo GetLengthInfo(Transform target)
        {
            float straightLength = Vector3.Distance(rodTip.position, target.position);
            float lineLength = lineBody.TotalLength;
            Vector3 lineDirection = Vector3.zero;

            var segments = lineBody.Segments;
            for (int i = 1; i < segments.Count; ++i)
            {
                if (segments[i].IsPinned || segments[i].kind == SegmentKind.Bobber)
                {
                    lineLength = lineBody.TotalLength - segments[i].height;
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

        public void SetLength(float length, float duration = 0f)
        {
            targetLength = totalLength = length;
            targetLengthTimer.Reset(duration);

            // 데이터 설정
            player.Model.SetLineLength(totalLength);
        }

        public float Release(float amount)
        {
            if (amount > 0f)
                totalLength += amount;

            player.Model.SetLineLength(totalLength);
            return targetLength = totalLength;
        }

        public float Reeling(float amount)
        {
            // 가장 높은 장비 점(찌 또는 손)까지는 감지 않음
            if (amount > 0f)
                totalLength = Mathf.Max(GetMinLength(), totalLength - amount);

            player.Model.SetLineLength(totalLength);
            return targetLength = totalLength;
        }



        // 이벤트 처리

        // Idle 진입 때 (처음, 회수 후 복귀 포함): 채비를 손에 든 상태로 되돌림
        protected override void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
            endPoint.SetTrackingTarget(null);
            lineBody.SetWaterContact(false);

            Transform leftHandle = param.type == CastingType.Pitching ? param.leftHandle : null;
            if (leftHandle != null)
            {
                AttachHandle(leftHandle);
            }
            else
            {
                DetachHandle();
                SetLength(totalLength, 1f);
            }
        }

        // 손을 놓음 (바늘 점은 FishingHook 이 Detach 하면서 날아가는 바늘을 추적)
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            DetachHandle();
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
            SetLength(1, 1f);

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
            targetLength = 1.6f;
        }
    }
}
