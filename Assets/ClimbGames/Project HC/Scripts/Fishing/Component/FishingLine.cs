using System;
using System.ComponentModel;
using Unity.Cinemachine;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine : MonoBehaviour
    {
        [SerializeField] private FishingLineBody lineBody;
        [SerializeField] private FishingLineDrawer[] lineDrawers;
        [SerializeField] private FishingLinePoint bobberPoint;
        [SerializeField] private FishingLinePoint endPoint;
        [SerializeField] private FishingLinePoint handlePoint;
        [SerializeField] private float lineLength;
        [ReadOnly(true)]
        [SerializeField] private float lineTension = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float forcedTension = 0f;

        private Transform rodTip;
        private LinearTween lengthTween = new LinearTween(1f);
        private FishingLineController controller = new FishingLineController();

        public float Length => lineLength;
        public float Tension => lineTension;
        public FishingLineBody Body => lineBody;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLinePoint HandlePoint => handlePoint;
        public FishingLineState State => controller.CurrentType;

        public void Initialize(Transform rodTip, float length, float bobberHeight)
        {
            this.rodTip = rodTip;

            lengthTween.Reset(true);
            SetLength(Mathf.Max(0.5f, length));
            lineBody.BuildLine(rodTip.position, Length);

            bobberPoint.Initialize(this, bobberHeight);
            endPoint.Initialize(this);
            handlePoint.Initialize(this);

            controller.InitializeAndStart(this, FishingLineState.Hanging);
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
            // 라인 길이 업데이트
            lengthTween.Update(deltaTime);
            float targetLength = handlePoint.HasTarget ? Vector3.Distance(rodTip.position, handlePoint.transform.position) : lineLength;
            lineBody.SetLength(Mathf.Lerp(lineBody.VisualLength, targetLength, lengthTween.Value));
            UpdateTension();

            // 물리 업데이트
            lineBody.Simulate(deltaTime);
            lineBody.ApplyConstraint(rodTip.position);

            // 컨트롤 포인트 업데이트
            bobberPoint.UpdatePosition();
            handlePoint.UpdatePosition();
            endPoint.UpdatePosition();
        }

        public void UpdateTension()
        {
            lineTension = 0f;
            FishingLinePoint tensionPoint = GetTensionPoint();
            if (tensionPoint.HasTarget)
            {
                // 직선 길이
                float straightLength = Vector3.Distance(rodTip.position, tensionPoint.transform.position);
                // 라인
                float lineLength = lineBody.CalculateLength(tensionPoint.PinPoint.currentIndex);
                // 텐션 길이
                float tensionLength = straightLength * 0.9f/*TensionFactor*/;
                lineTension = Mathf.Max(0f, (straightLength - lineLength) / (straightLength - tensionLength));
                lineTension /= 5f/*MaxTension*/;
            }
        }

        public void SetLength(float length, bool useTween = false)
        {
            lineLength = length;
            lengthTween.Reset(useTween == false);
        }

        public FishingLinePoint GetTensionPoint()
        {
            FishingLinePoint tensionTarget = endPoint;
            if (bobberPoint.HasTarget && bobberPoint.Height > tensionTarget.Height)
                tensionTarget = bobberPoint;

            if (handlePoint.HasTarget && handlePoint.Height > tensionTarget.Height)
                tensionTarget = handlePoint;

            return tensionTarget;
        }

        public void SetCastingType(CastingType type, Transform target)
        {
            if (type == CastingType.Pitching)
            {
                handlePoint.SetTrackingTarget(target, true);
            }
            else
            {
                handlePoint.SetTrackingTarget(null);
                lengthTween.Reset();
            }
        }

        public void Release(float amount)
        {
            if (amount > 0f)
                lineLength += amount;
        }

        public void Reeling(float amount)
        {
            if (amount > 0f)
                lineLength = Mathf.Max(0.5f, lineLength - amount);
        }
    }
}