using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLineHandle : FishingLinePoint
    {
        [SerializeField] private Transform pinnedTarget;

        private LinearTween targetTween = new LinearTween(0.4f);
        private FishingLineHandleController controller = new FishingLineHandleController();

        public bool HasTarget => pinnedTarget != null;
        public bool HasForce => HasTarget || Body.isKinematic == false;
        public Transform Target => pinnedTarget;
        public FishingLineHandleState State => controller.CurrentType;
        public override Vector3 Up => (line.GetPosition(line.PositionCount - 2) - transform.position).normalized;

        void FixedUpdate()
        {
            targetTween.Update(Time.fixedDeltaTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            controller.ChangeState(FishingLineHandleState.Settled);
        }

        public override void Initialize(FishingLine line)
        {
            base.Initialize(line);

            Body.isKinematic = true;
            controller.InitializeAndStart(this, FishingLineHandleState.Hanging);
        }

        public void SetTarget(Transform target)
        {
            pinnedTarget = target;
            targetTween.Reset();
        }

        public override void UpdatePosition()
        {
            if (HasForce)
            {
                if (pinnedTarget != null)
                {
                    Vector3 targetPosition = pinnedTarget.position;
                    transform.position = Vector3.Lerp(transform.position, targetPosition, targetTween.Value);
                }

                line.SetPosition(line.PositionCount - 1, transform.position);
            }
            else
            {
                transform.position = line.GetPosition(line.PositionCount - 1);
                transform.up = Up;
            }
        }

        public void CastingThrow(Vector3 position, float angle)
        {
            if (TryGetThrowVelocity(position, angle, out var velocity))
            {
                Body.isKinematic = false;
                Body.useGravity = true;
                Body.excludeLayers = 1 << FishingLayer.Water;
                Body.linearDamping = 0f;
                Body.linearVelocity = velocity;

                SetTarget(null);
                controller.ChangeState(FishingLineHandleState.Flying);
            }
        }

        bool TryGetThrowVelocity(Vector3 targetPosition, float angle, out Vector3 result)
        {
            Vector3 direction = targetPosition - transform.position;
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