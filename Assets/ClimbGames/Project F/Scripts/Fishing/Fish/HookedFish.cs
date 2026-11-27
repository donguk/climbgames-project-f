using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 바늘을 문 물고기: 자기 위치(transform) 하나로 움직임
    // 이동 = 헤엄(의도, 수역 안으로 제한) + 줄 복원(줄 물리의 실제 장력만큼 줄 방향으로)
    public class HookedFish : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField, Min(0f)] private float rotationSpeed = 6.0f;
        [SerializeField, Range(0f, 180f)] private float steerAngleThreshold = 180f;
        [SerializeField, Min(0f)] private float depthGain = 2f;            // 목표 깊이로 붙는 정도 (1/s)
        [SerializeField, Min(0.1f)] private float maxRestoreSpeed = 10f;   // 줄에 끌려오는 최대 속도 (m/s)

        private FishingSpot spot;
        private FishDepthSetting depth;
        private float verticalSpeed = 1f;
        private FishIntentInfo intent;

        // 줄 끝(바늘) 구간의 실제 장력: 위쪽 줄 방향, 힘(N), 그 힘으로 줄이 늘어난 양(m, 한 스텝 최대 이동 제한용)
        private Vector3 lineDirection;
        private float lineForce;
        private float lineStretch;

        private Rigidbody body;

#if UNITY_EDITOR
        private float debugTopY;
        private float debugFloorY;
#endif

        public Transform ModelRoot => modelRoot;
        public FishIntentInfo Intent => intent;
        public Rigidbody Body
        {
            get
            {
                if (body == null)
                    body = gameObject.GetOrAddComponent<Rigidbody>();
                return body;
            }
        }

        public void Initialize()
        {
            modelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            intent = default;
            ClearLineForce();
        }

        // 헤엄칠 수역과 깊이 설정
        public void SetWater(FishingSpot spot, in FishDepthSetting depth, float verticalSpeed)
        {
            this.spot = spot;
            this.depth = depth;
            this.verticalSpeed = Mathf.Max(0.01f, verticalSpeed);
        }

        public void SetIntent(in FishIntentInfo intent)
        {
            this.intent = intent;
        }

        // 줄 물리가 물고기를 당기는 힘: direction = 바늘 점에서 위쪽 줄 점 방향, force = 그 구간 장력 (N)
        // stretch = 그 힘으로 줄이 늘어난 양 (m): 한 스텝에 이보다 많이 끌려오면 느슨해졌다 다시 당겨지며 떨림
        public void SetLineForce(Vector3 direction, float force, float stretch)
        {
            lineDirection = direction.sqrMagnitude > 1e-8f ? direction.normalized : Vector3.zero;
            lineForce = float.IsFinite(force) ? Mathf.Max(0f, force) : 0f;
            lineStretch = float.IsFinite(stretch) ? Mathf.Max(0f, stretch) : 0f;
        }

        public void ClearLineForce()
        {
            lineDirection = Vector3.zero;
            lineForce = 0f;
            lineStretch = 0f;
        }

        void FixedUpdate()
        {
            // 줄에 조인트로 매달린 뒤(회수)에는 물리가 움직임
            if (body != null && body.isKinematic == false)
                return;

            if (spot == null)
                return;

            float dt = Time.fixedDeltaTime;
            Vector3 position = transform.position;

            // 높이 범위
            spot.GetDepthRange(position, depth, out float topY, out float floorY);
            floorY = Mathf.Min(floorY, topY);
#if UNITY_EDITOR
            debugTopY = topY;
            debugFloorY = floorY;
#endif

            // 1. 목표 깊이로 상하 속도
            float targetY = Mathf.Lerp(topY, floorY, intent.depthRatio);
            float verticalVelocity = Mathf.Clamp((targetY - position.y) * depthGain, -verticalSpeed, verticalSpeed);

            // 2. 회전: heading(+상하) 쪽으로 부드럽게
            Vector3 heading = intent.heading;
            heading.y = 0f;
            if (heading.sqrMagnitude > 0.0001f)
            {
                heading.Normalize();
                Vector3 lookDirection = heading * Mathf.Max(intent.swimSpeed, 0.5f) + Vector3.up * verticalVelocity;
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);

                float angle = Quaternion.Angle(transform.rotation, targetRotation);
                transform.rotation = angle <= steerAngleThreshold
                    ? Quaternion.Slerp(transform.rotation, targetRotation, dt * rotationSpeed)
                    : targetRotation;
            }

            // 3. 헤엄: 지금 바라보는 수평 방향으로 (자기 헤엄으로는 수역 밖, 수면 위, 바닥 아래로 못 감)
            Vector3 forward = transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.zero;

            Vector3 swimVelocity = forward * intent.swimSpeed + Vector3.up * verticalVelocity;
            Vector3 next = position + swimVelocity * dt;
            if (spot.IsInsideAreas(position))
                next = spot.ClampInAreas(next);
            if (position.y <= topY)
                next.y = Mathf.Min(next.y, topY);
            next.y = Mathf.Max(next.y, floorY);

            // 4. 줄 복원: 줄의 실제 장력만큼 위쪽 줄 방향으로 (수역/수면 제한 없음, 감으면 끌려 나옴)
            //    한 스텝 이동은 늘어난 양의 절반까지 (지나쳐서 느슨해졌다 다시 당겨지는 떨림 방지)
            if (lineForce > 0f && lineDirection != Vector3.zero)
            {
                float restoreSpeed = Mathf.Min(maxRestoreSpeed, lineForce * intent.restoreGain);
                next += lineDirection * Mathf.Min(restoreSpeed * dt, lineStretch * 0.5f);
            }

            // 바닥은 뚫지 않음
            next.y = Mathf.Max(next.y, floorY);
            transform.position = next;
        }

        // 줄 점에 조인트로 매달림 (위치는 그대로, 회전만 점에 맞춤)
        public void AttachToLine(FishingLinePoint point)
        {
            Body.isKinematic = false;

            var joint = GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = gameObject.AddComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                joint.connectedBody = null;
            }

            // 초기 회전 리셋
            transform.rotation = point.transform.rotation;
            joint.connectedBody = point.Body;

            joint.angularXDrive = new JointDrive() { positionDamper = 0.1f, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 0.1f, maximumForce = float.MaxValue };

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            if (spot == null)
                return;

            Vector3 position = transform.position;

            // 헤엄 방향
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(position, position + intent.heading * Mathf.Max(0.5f, intent.swimSpeed));

            // 수면 ~ 바닥 범위
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(new Vector3(position.x, debugTopY, position.z), new Vector3(position.x, debugFloorY, position.z));
            UnityEditor.Handles.color = Color.blue;
            UnityEditor.Handles.DrawWireDisc(new Vector3(position.x, debugFloorY, position.z), Vector3.up, 0.15f);

            UnityEditor.Handles.Label(position + Vector3.up * 0.3f, intent.state.ToString());
        }
#endif
    }
}
