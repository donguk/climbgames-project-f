using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class HookedFish : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform struggleTarget;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float rotationSpeed = 6.0f;
        [SerializeField] private float steerAngleThreshold = 180f;
        [SerializeField] private float closeDistanceThreshold = 0.1f;
        [SerializeField] private Vector2 heightLimit = new Vector2(0f, 0.2f);
        [SerializeField] private float lifeTime;

        private FishController controller;
        private FishData fishData;
        private float hookHeight;
        private float prevDistance;
        private Vector3 targetPosition;
        private WaterAreaInfo areaInfo;

        public Transform ModelRoot => modelRoot;
        public Transform StruggleTarget => struggleTarget;
        public Vector3 StruggleDirection => transform.position - struggleTarget.position;

        public void Initialize(FishController controller, FishData data)
        {
            this.controller = controller;
            fishData = data;

            hookHeight = transform.position.y;
            targetPosition = transform.position;
            modelRoot.localPosition = Vector3.zero;
            struggleTarget.localPosition = Vector3.zero;
        }

        void FixedUpdate()
        {
            if (controller.CurrentType == FishState.Struggle)
            {
                SimulateStruggle();

                //
            }
        }

        void SimulateStruggle()
        {
            // 위치 재설정
            float distance = Vector3.Distance(modelRoot.position, targetPosition);
            if (prevDistance < distance || distance < 0.1f)
            {
                Vector2 randomCircle = Random.insideUnitCircle * areaInfo.range;
                targetPosition = areaInfo.center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                targetPosition.y = hookHeight + Random.Range(heightLimit.x, heightLimit.y);

                // 위치 보정
                if (Vector3.Distance(modelRoot.position, targetPosition) < 0.1f)
                    targetPosition += (targetPosition - modelRoot.position).normalized * 0.1f;
            }
            prevDistance = distance;

            // 이동
            Vector3 directionToTarget = targetPosition - modelRoot.position;
            distance = directionToTarget.magnitude;

            float maxMoveStep = moveSpeed * Time.fixedDeltaTime;
            if (distance <= maxMoveStep)
            {
                modelRoot.SetPositionAndRotation(targetPosition, Quaternion.LookRotation(directionToTarget.normalized));
                return;
            }

            Vector3 normalizedDirection = directionToTarget.normalized;
            float angle = Vector3.Angle(modelRoot.forward, normalizedDirection);
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection);

            // 제한 각도 보다 작고 먼 경우는 부드럽게 이동
            if (angle <= steerAngleThreshold && distance > closeDistanceThreshold)
            {
                modelRoot.rotation = Quaternion.Slerp(
                    modelRoot.rotation,
                    targetRotation,
                    Time.fixedDeltaTime * rotationSpeed
                );
            }
            else
            {
                modelRoot.rotation = targetRotation;
            }

            // 정면(forward)으로 이동
            modelRoot.position += modelRoot.forward * moveSpeed * Time.fixedDeltaTime;
        }

        public void SetAreaInfo(WaterAreaInfo areaInfo)
        {
            this.areaInfo = areaInfo;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.DrawWireDisc(areaInfo.center, Vector3.up, areaInfo.range);
        }
#endif
    }
}