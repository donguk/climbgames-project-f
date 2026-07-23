using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class HookedFish : MonoBehaviour, ILineAttachable
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform moveTarget;
        [SerializeField] private float rotationSpeed = 6.0f;
        [SerializeField] private float steerAngleThreshold = 180f;
        [SerializeField] private float closeDistanceThreshold = 0.1f;
        [SerializeField] private Vector2 heightLimit = new Vector2(0f, 0.2f);

        private FishController controller;
        private float hookHeight;
        private float prevDistance;
        private Vector3 targetPosition;
        private FishStruggleInfo struggleInfo;
        private LineTensionInfo tensionInfo;
        private WaterAreaInfo areaInfo;
        private Rigidbody body;

        public Transform ModelRoot => modelRoot;
        public Transform MoveTarget => moveTarget;
        public Transform Target => transform;
        public Rigidbody Body
        {
            get
            {
                if (body == null)
                    body = gameObject.GetOrAddComponent<Rigidbody>();
                return body;
            }
        }

        public void Initialize(FishController controller)
        {
            this.controller = controller;

            hookHeight = transform.position.y;
            targetPosition = transform.position;
            modelRoot.localPosition = Vector3.zero;
            moveTarget.localPosition = Vector3.zero;
        }

        void FixedUpdate()
        {
            if (controller.CurrentType == FishState.Struggle)
            {
                SimulatePull();
                SimulateStruggle();

                struggleInfo.Reset();
                tensionInfo.Reset();
            }
        }

        void SimulatePull()
        {
            // 물고기의 목표 텐션을 위한 움직임
            Vector3 lineDir = tensionInfo.direction;
            if (lineDir.sqrMagnitude > 0.0001f)
            {
                Vector3 veclocity = lineDir.normalized * tensionInfo.intensity;
                moveTarget.position += veclocity * Time.fixedDeltaTime;
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

            float maxMoveStep = struggleInfo.speed * Time.fixedDeltaTime;
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
            modelRoot.position += modelRoot.forward * struggleInfo.speed * Time.fixedDeltaTime;
        }

        public void SetStruggleInfo(FishStruggleInfo struggleInfo)
        {
            this.struggleInfo = struggleInfo;
            areaInfo = struggleInfo.areaInfo;
        }

        public void SetTensionInfo(LineTensionInfo tensionInfo)
        {
            this.tensionInfo = tensionInfo;
        }

        void ILineAttachable.OnAttached(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXDrive = new JointDrive() { positionDamper = 0.1f, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 0.1f, maximumForce = float.MaxValue };
        }

        void ILineAttachable.OnDetached(FishingLinePoint point)
        {
            //
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