using System;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishingLine : FishingEquipment
    {
        [SerializeField] private FishingLineBody lineBody;
        [SerializeField] private FishingLinePoint bobberPoint;
        [SerializeField] private FishingLinePoint endPoint;
        [SerializeField] private FishingLinePoint handlePoint;
        [SerializeField] private float totalLength;
        [SerializeField] private bool showDebug;

        private FishingLineDrawer[] lineDrawers;
        private FishingPlayer player;
        private Transform rodTip;
        private float targetLength;
        private NormalizedTimer targetLengthTimer = new NormalizedTimer(1f);

        public float TotalLength => totalLength;
        public FishingLineBody Body => lineBody;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLinePoint HandlePoint => handlePoint;

        public void Initialize(FishingPlayer player, Transform rodTip, float length, float bobberHeight)
        {
            this.player = player;
            this.rodTip = rodTip;
            lineDrawers = GetComponentsInChildren<FishingLineDrawer>();

            SetLength(Mathf.Max(1f, length));
            lineBody.BuildLine(rodTip.position, TotalLength, bobberHeight);

            bobberPoint.Initialize(this, bobberHeight);
            bobberPoint.BindBodyPoint(lineBody.BobberPoint);
            endPoint.Initialize(this);
            handlePoint.Initialize(this);
        }

        void Update()
        {
            // pinned 된 부분으로 나눠서 그리기
            int startIndex = 0, drawerIndex = 0;
            var linePoints = lineBody.Points;
            for (int i = 1; i < linePoints.Count; ++i)
            {
                var linePoint = linePoints[i];
                if (linePoint.IsPinned || linePoint.isBobber || i >= linePoints.Count - 1)
                {
                    if (drawerIndex < lineDrawers.Length)
                    {
                        lineDrawers[drawerIndex].gameObject.SetActive(true);
                        lineDrawers[drawerIndex].DrawLine(lineBody.Positions, startIndex, i - startIndex + 1);
                    }

                    startIndex = i;
                    drawerIndex++;
                }
            }

            // 사용안하는 라인 끄기
            for (int i = drawerIndex; i < lineDrawers.Length; ++i)
                lineDrawers[i].gameObject.SetActive(false);
        }

        public void UpdateLine(float deltaTime)
        {
            // 길이 업데이트
            targetLengthTimer.Update(deltaTime);
            lineBody.SetLength(Mathf.Lerp(lineBody.VisualLength, targetLength, targetLengthTimer.Value));

            // 물리 업데이트 (찌는 줄의 찌 점을 따라가므로 따로 반영할 위치가 없음)
            lineBody.Step(deltaTime, rodTip.position);

            // 컨트롤 포인트 업데이트
            bobberPoint.UpdatePosition();
            handlePoint.UpdatePosition();
            endPoint.UpdatePosition();
        }

#if UNITY_EDITOR
        // 팁부터 첫 고정점까지 줄이 늘어난 정도와 고정점이 끌린 속도
        void OnGUI()
        {
            if (showDebug == false || lineBody == null)
                return;

            float rest = lineBody.PinRestLength;
            float stretch = lineBody.PinStretch;
            float ratio = rest > 0f ? stretch / rest * 100f : 0f;

            Vector3 move = lineBody.PinDisplacement;
            move.y = 0f;
            float deltaTime = lineBody.LastDeltaTime;
            float pinSpeed = deltaTime > 0f ? move.magnitude / deltaTime : 0f;

            GUI.Label(new Rect(10f, 10f, 400f, 80f),
                $"Rest: {rest:F2} m\nStretch: {stretch:F3} m ({ratio:F2} %)\nPinMove: {pinSpeed:F3} m/s");
        }
#endif

        public LineLengthInfo GetLengthInfo(Transform target)
        {
            // 직선 길이
            float straightLength = Vector3.Distance(rodTip.position, target.position);
            float lineLength = 0f;
            Vector3 lineDirection = Vector3.zero;

            var points = lineBody.Points;
            for (int i = 0; i < points.Count; ++i)
            {
                lineLength += points[i].segmentLength;
                if (i > 0)
                {
                    if (points[i].IsPinned || points[i].isBobber)
                    {
                        lineDirection = (rodTip.transform.position - lineBody.GetPosition(points[i].currentIndex)).normalized;
                        break;
                    }
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
            if (amount > 0f)
                totalLength = Mathf.Max(bobberPoint.Height + lineBody.SegmentLength, totalLength - amount); // 찌 점까지는 감지 않음

            player.Model.SetLineLength(totalLength);
            return targetLength = totalLength;
        }



        // 이벤트 처리
        protected override void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
            if (param.type == CastingType.Pitching)
            {
                handlePoint.SetTrackingTarget(param.leftHandle, 1f);
                targetLength = 2f;
            }
            else
            {
                handlePoint.SetTrackingTarget(null);
                SetLength(totalLength, 1f);
            }
        }

        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            // 피칭모드 일 경우 그립 풀기
            handlePoint.SetTrackingTarget(null);
        }

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
            // 추적 끈기
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

            endPoint.SetTrackingTarget(param.rod.LineGuide, 1f, () => endPoint.SetTrackingTarget(null));
            targetLength = 1.6f;
        }
    }
}