using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine : MonoBehaviour
    {
        [SerializeField] private FishingLineBody lineBody;
        [SerializeField] private FishingLinePoint bobberPoint;
        [SerializeField] private FishingLinePoint endPoint;
        [SerializeField] private FishingLinePoint handlePoint;
        [SerializeField] public float slackDistance = 1f;
        [SerializeField] public float maxPullDistance = 0.5f;
        [SerializeField] private float totalLength;
        [Range(0f, 1f)]
        [SerializeField] private float forcedTension = 0f;

        private FishingLineDrawer[] lineDrawers;
        private Transform rodTip;
        private float targetLength, targetTension;
        private NormalizedTimer targetLengthTimer = new NormalizedTimer(1f);
        private NormalizedTimer forcedTensionTimer = new NormalizedTimer(1f);

        public float TotalLength => totalLength;
        public float TensionLength { get; private set; }
        public FishingLineBody Body => lineBody;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLinePoint HandlePoint => handlePoint;

        public void Initialize(Transform rodTip, float length, float bobberHeight)
        {
            this.rodTip = rodTip;
            lineDrawers = GetComponentsInChildren<FishingLineDrawer>();

            SetLength(Mathf.Max(0.5f, length));
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
            lineBody.Simulate(deltaTime);
            lineBody.ApplyConstraint(rodTip.position);

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
        }

        public void SetForcedTension(float tension, float duration = 0f)
        {
            forcedTension = targetTension = tension;
            forcedTensionTimer.Reset(duration);
        }

        public void SetCastingType(CastingType type, Transform target)
        {
            if (type == CastingType.Pitching)
            {
                handlePoint.SetTrackingTarget(target, true);
                targetLength = Vector3.Distance(rodTip.position, target.position);
            }
            else
            {
                handlePoint.SetTrackingTarget(null);
                SetLength(totalLength, 1f);
            }
        }

        public float Release(float amount)
        {
            if (amount > 0f)
                totalLength += amount;

            return targetLength = totalLength;
        }

        public float Reeling(float amount)
        {
            if (amount > 0f)
                totalLength = Mathf.Max(0.5f, totalLength - amount);

            return targetLength = totalLength;
        }
    }
}