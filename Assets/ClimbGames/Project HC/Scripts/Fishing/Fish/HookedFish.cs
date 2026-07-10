using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class HookedFish : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 6.0f;
        [SerializeField] private float steerAngleThreshold = 180f;
        [SerializeField] private float closeDistanceThreshold = 0.1f;
        [SerializeField] private Vector2 heightLimit = new Vector2(0f, 0.2f);

        private FishController controller;
        private float biteHeight;
        private Vector3 movePosition;

        public void Initialize(FishController controller)
        {
            this.controller = controller;
            biteHeight = transform.position.y;
        }

        void FixedUpdate()
        {
            if (controller.CurrentType == FishState.Struggle)
            {
                float currentSpeed = moveSpeed; // 체력에 따라 속도 조절
                UpdateMovement(movePosition, currentSpeed);
            }
        }

        public void UpdateMovement(Vector3 targetPosition, float moveSpeed)
        {
            Vector3 directionToTarget = targetPosition - transform.position;
            float distance = directionToTarget.magnitude;

            if (distance < 0.001f)
                return;

            float maxMoveStep = moveSpeed * Time.fixedDeltaTime;
            if (distance <= maxMoveStep)
            {
                transform.SetPositionAndRotation(targetPosition, Quaternion.LookRotation(directionToTarget.normalized));
                return;
            }

            Vector3 normalizedDirection = directionToTarget.normalized;
            float angle = Vector3.Angle(transform.forward, normalizedDirection);
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);

            // 제한 각도 보다 작고 먼 경우는 부드럽게 이동
            if (angle <= steerAngleThreshold && distance > closeDistanceThreshold)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * rotationSpeed
                );
            }
            else
            {
                transform.rotation = targetRotation;
            }

            // 정면(forward)으로 이동
            transform.position += transform.forward * moveSpeed * Time.fixedDeltaTime;
        }

        public void SetMovePosition(Vector3 position)
        {
            movePosition = position;

            // 높이 설정
            movePosition.y = biteHeight + Random.Range(heightLimit.x, heightLimit.y);
        }
    }
}