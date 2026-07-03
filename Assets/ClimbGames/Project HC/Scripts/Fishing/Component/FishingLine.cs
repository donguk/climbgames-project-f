using System;
using System.ComponentModel;
using R3;
using R3.Triggers;
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
        [SerializeField] private float totalLength;
        [Range(0f, 1f)]
        [ReadOnly(true)]
        [SerializeField] private float lineTension = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float forcedTension = 0f;

        private Transform rodTip;
        private LinearTween lengthTween = new LinearTween(0.5f);
        private FishingLineController controller = new FishingLineController();

        public FishingLineBody Body => lineBody;
        public float Length => totalLength;
        public float Tension => lineTension;
        public Transform RodTip => rodTip;
        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint EndPoint => endPoint;
        public FishingLineState LineState => controller.CurrentType;

        void Awake()
        {
            endPoint.Body.OnCollisionEnterAsObservable().Subscribe(_ =>
            {
                controller.ChangeState(FishingLineState.Settled);
            }).AddTo(this);
        }

        public void Initialize(Transform rodTip, float length, float bobberHeight)
        {
            this.rodTip = rodTip;

            lineBody.BuildLine(rodTip.position, totalLength = Mathf.Max(0.5f, length));
            bobberPoint.Initialize(bobberHeight);
            endPoint.Initialize();
            handlePoint.Initialize();

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
            if (handlePoint.HasTarget)
            {
                lineBody.SetLength(Vector3.Distance(rodTip.position, handlePoint.transform.position));
            }
            else
            {
                lengthTween.Update(deltaTime);
                lineBody.SetLength(Mathf.Lerp(lineBody.VisualLength, totalLength, lengthTween.Value));
            }

            // 물리 업데이트
            lineBody.Simulate(deltaTime);
            lineBody.ApplyConstraint(rodTip.position);

            // 컨트롤 포인트 업데이트
            bobberPoint.UpdatePosition(this);
            handlePoint.UpdatePosition(this);
            endPoint.UpdatePosition(this);
        }

        //public void UpdateSegment()
        //{
        //    // 직선 거리의 길이
        //    float straightLength = Vector3.Distance(rodTip.position, lineHandle.transform.position);
        //    float defaultSegment = straightLength / (mainLine.PositionCount - 1);
        //
        //    // 논리적 거리
        //    float visualLength = lineHandle.HasTarget ? straightLength : totalLength;
        //    float targetSegment = visualLength / (mainLine.PositionCount - 1);
        //
        //    lineTension = 0f;
        //    if (lineHandle.HasForce)
        //    {
        //        // 텐션이 느껴지는 길이
        //        float tensionLength = defaultSegment * 0.9f;
        //        if (targetSegment > tensionLength)
        //            targetSegment = Mathf.Lerp(targetSegment, tensionLength, forcedTension);
        //
        //        // 현재 라인의 텐션 정도 업데이트
        //        lineTension = Mathf.Clamp01((defaultSegment - targetSegment) / (defaultSegment - tensionLength));
        //    }
        //
        //    // 목표 텐션 보간
        //    //segmentLength = Mathf.Lerp(segmentLength, targetSegment, segmentTween.Value);
        //}

        public void SetLength(float length)
        {
            totalLength = length;
        }

        public void SetHandleTarget(Transform target)
        {
            if (target != null)
            {
                // 물리 포인트 추가
                FishingLineBody.LinePoint pinnedPoint = lineBody.AddPinPoint(handlePoint);
                handlePoint.SetTrackingTarget(target, pinnedPoint);
                lengthTween.Reset(true);
            }
            else
            {
                // 추가된 물리 포인트 삭제
                if (handlePoint.PinnedPoint != null)
                    lineBody.RemovePinPoint(handlePoint.PinnedPoint.currentIndex);

                handlePoint.SetTrackingTarget(null);
                lengthTween.Reset();
            }
        }

        public void Release(float amount)
        {
            if (amount > 0f)
                totalLength += amount;
        }

        public void Reeling(float amount)
        {
            if (amount > 0f)
                totalLength = Mathf.Max(0.5f, totalLength - amount);
        }

        public void CastingThrow(Vector3 position, float angle)
        {
            if (TryGetThrowVelocity(endPoint.transform, position, angle, out var velocity))
            {
                handlePoint.SetTrackingTarget(null);
                var pinnedPoint = lineBody.AddPinPoint(endPoint);
                endPoint.SetThrowVelocity(velocity, pinnedPoint);

                controller.ChangeState(FishingLineState.Flying);
            }
        }

        bool TryGetThrowVelocity(Transform target, Vector3 position, float angle, out Vector3 result)
        {
            Vector3 direction = position - target.position;
            float h = direction.y; // 높이 차 (yOffset)
            direction.y = 0; // 수평 거리 계산을 위해 y축 제거
            float d = direction.magnitude; // 수평 거리 (distance)
            float a = angle * Mathf.Deg2Rad; // 라디안 변환
            float g = Physics.gravity.magnitude;

            // 포물선 속도 공식 (분모 0 체크 포함)
            float cosA = Mathf.Cos(a);
            float v2 = (g * d * d) / (2 * cosA * cosA * (d * Mathf.Tan(a) - h));

            if (v2 <= 0)
            {
                Debug.LogWarning("invalid throw angle..");
                result = Vector3.zero;
                return false;
            }

            float v = Mathf.Sqrt(v2);

            // 발사 벡터 생성
            // 1. 수평 방향으로 정규화된 벡터
            Vector3 launchVec = direction.normalized;

            // 2. 수평 벡터를 위쪽(Y)으로 '각도'만큼 회전
            // 수평 성분은 cos(a), 수직 성분은 sin(a)의 비율을 가집니다.
            Vector3 finalVelocity = launchVec * cosA;
            finalVelocity.y = Mathf.Sin(a);

            // 3. 최종 속도 크기 곱하기
            result = finalVelocity * v;
            return true;
        }
    }
}