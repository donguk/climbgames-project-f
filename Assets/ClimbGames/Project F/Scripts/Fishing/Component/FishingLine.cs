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
        [Range(0f, 1f)]
        [SerializeField] private float forcedTension = 0f;

        private FishingLineDrawer[] lineDrawers;
        private FishingPlayer player;
        private Transform rodTip;
        private float targetLength, targetTension;
        private NormalizedTimer targetLengthTimer = new NormalizedTimer(1f);
        private NormalizedTimer forcedTensionTimer = new NormalizedTimer(1f);

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
            lineBody.BuildLine(rodTip.position, TotalLength);

            bobberPoint.Initialize(this, bobberHeight);
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
                if (linePoint.IsPinned || i >= linePoints.Count - 1)
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

            // 강제 텐션
            forcedTensionTimer.Update(deltaTime);
            lineBody.SetForcedTension(Mathf.Lerp(lineBody.ForcedTension, targetTension, forcedTensionTimer.Value));

            // 물리 업데이트
            lineBody.Step(deltaTime, rodTip.position);

            // 컨트롤 포인트 업데이트
            bobberPoint.UpdatePosition();
            handlePoint.UpdatePosition();
            endPoint.UpdatePosition();
        }

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
                    if (points[i].IsPinned)
                    {
                        lineDirection = (rodTip.transform.position - lineBody.GetPosition(points[i].currentIndex)).normalized;
                        break;
                    }
                }
            }

            // 라인이 당겨진 길이
            float pullLength = Mathf.Max(0f, straightLength - 1.5f/*slack length*/ - lineLength);
            float pullSpeed = pullLength * 4f;

            return new LineLengthInfo()
            {
                straightLength = straightLength,
                lineLength = lineLength,
                direction = lineDirection,
                pullLength = pullLength,
                pullSpeed = pullSpeed,
            };
        }

        public void SetLength(float length, float duration = 0f)
        {
            targetLength = totalLength = length;
            targetLengthTimer.Reset(duration);

            // 데이터 설정
            player.Model.SetLineLength(totalLength);
        }

        public void SetForcedTension(float tension, float duration = 0f)
        {
            forcedTension = targetTension = tension;
            forcedTensionTimer.Reset(duration);
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
                totalLength = Mathf.Max(0.5f, totalLength - amount);

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
            lineBody.SetWaterSurface(param.waterHeight);
        }

        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            // 물고기 추적
            endPoint.SetTrackingTarget(param.hookedFish.ModelRoot);
            SetForcedTension(1f, 0.5f);

            // 물고기를 향해 물속으로 들어가는 줄은 수면에 걸리지 않음
            lineBody.SetWaterContact(false);
        }

        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            // 추적 끈기
            endPoint.SetTrackingTarget(null);
            SetForcedTension(0f, 0.1f);
            lineBody.SetWaterContact(true);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            SetForcedTension(0f, 0.1f);
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