using UnityEngine;
using R3;
using System;

namespace ClimbGames.ProjectF
{
    public class FishingBobber : FishingLineObject
    {
        [Serializable]
        public class FloatingClip
        {
            public AnimationCurve height;
            public float duration;
        }

        [SerializeField] private Transform modelRoot;
        [SerializeField] private FloatingClip landingClip;
        [SerializeField] private FloatingClip floatingClip;

        [Header("Line Force")]
        [SerializeField] private float mass = 1f;
        [SerializeField] private float waterDrag = 2f;
        [SerializeField] private float staticForce = 0.2f;

        [SerializeField] private float tiltMaxForce = 1.5f;
        [SerializeField] private float tiltSpeed = 10f;
        [SerializeField] private float tiltRecoverSpeed = 0.5f;

        [Header("Break Out")]
        [SerializeField] private float liftStiffness = 40f;
        [SerializeField] private float liftDamping = 8f;
        [SerializeField] private float breakoutHeight = 0.25f;

        [Header("Landing")]
        [SerializeField] private float landHeightOffset = 0.02f;

        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float pullMaxAngle = 80f;
        [SerializeField] private float biteMaxAngle = 90f;

        private float waterHeight;
        private Vector3 velocity;
        private Vector3 lineForce;
        private bool isLanding;
        private float lift;
        private float liftVelocity;
        private Quaternion currentDragRotation = Quaternion.identity;
        private Quaternion currentBiteRotation = Quaternion.identity;
        private NormalizedTimer landingTimer = new NormalizedTimer();
        private NormalizedTimer floatingTimer = new NormalizedTimer();
        private FishBiteInfo biteInfo;

        public bool IsFloating => RuntimeJoint == null;

        // 착수 연출이 끝난 뒤에만 분리 가능
        public bool ShouldBreakOut => IsFloating && landingTimer.IsCompleted && lift > breakoutHeight;

        void FixedUpdate()
        {
            if (IsFloating)
            {
                SimulatePull();
                SimulateRotation();
                SimulateHeight();

                lineForce = Vector3.zero;
                biteInfo.Reset();
            }
            else if (isLanding)
            {
                UpdateLanding();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[FishingBobber] OnTggerEnter: {other.gameObject.name}");
            if (other.gameObject.layer == FishingLayer.Water)
            {
                waterHeight = other.transform.position.y;
                isLanding = true;
            }
        }

        // 착수 전에 물에서 벗어나면 대기 취소
        void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == FishingLayer.Water)
                isLanding = false;
        }

        // 콜라이더가 찌보다 아래에 있으므로 찌가 수면에 왔을 때 착수함
        // 수면까지 못 내려오면 줄에 매달린 채로 기다림
        void UpdateLanding()
        {
            if (transform.position.y <= waterHeight + landHeightOffset)
            {
                isLanding = false;
                LandOnWater();
            }
        }

        void LandOnWater()
        {
            landingTimer.Reset(landingClip.duration);

            LinePoint.Detach(this);
            Body.excludeLayers = 1 << FishingLayer.Water;

            MessageBroker.Publish(new FishingEvent.OnBobberEnterWater() { waterHeight = waterHeight });
        }

        void SimulateHeight()
        {
            float targetHeight = waterHeight;
            if (landingTimer.IsCompleted == false)
            {
                landingTimer.Update(Time.deltaTime);
                targetHeight += landingClip.height.Evaluate(landingTimer.Value);

                if (landingTimer.IsCompleted)
                {
                    MessageBroker.Publish(new FishingEvent.OnBobberLandedWater());
                    floatingTimer.Reset(floatingClip.duration);
                }
            }
            else
            {
                floatingTimer.Update(Time.deltaTime);
                targetHeight += floatingClip.height.Evaluate(floatingTimer.Value);

                if (floatingTimer.IsCompleted)
                    floatingTimer.Reset();

                targetHeight += biteInfo.bobberHeight;
            }

            // 연출 높이
            Vector3 position = transform.position;
            float baseHeight = Mathf.Lerp(position.y - lift, targetHeight, Time.deltaTime * 4f);

            // 줄이 위로 당기는 힘과 수면으로 되돌리는 힘으로 정해지는 높이
            float deltaTime = Time.fixedDeltaTime;
            liftVelocity += (lineForce.y - liftStiffness * lift) / mass * deltaTime;
            liftVelocity /= 1f + liftDamping * deltaTime;
            lift += liftVelocity * deltaTime;

            position.y = baseHeight + lift;
            transform.position = position;
        }

        // 정지 마찰보다 약한 수평 힘은 무시함
        Vector3 GetHorizontalForce()
        {
            Vector3 force = lineForce;
            force.y = 0f;
            return force.magnitude < staticForce ? Vector3.zero : force;
        }

        void SimulatePull()
        {
            float deltaTime = Time.fixedDeltaTime;

            velocity += GetHorizontalForce() / mass * deltaTime;
            velocity /= 1f + waterDrag * deltaTime;
            transform.position += velocity * deltaTime;
        }

        void SimulateRotation()
        {
            Quaternion dargRotation = Quaternion.identity;
            // 기울임은 정지 마찰과 상관없이 약한 힘에도 반응함
            Vector3 force = lineForce;
            force.y = 0f;
            float forceSize = force.magnitude;

            if (forceSize > 0.0001f)
            {
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, force / forceSize);
                float dragAngle = pullMaxAngle * Mathf.Clamp01(forceSize / tiltMaxForce);
                dargRotation = Quaternion.AngleAxis(dragAngle, tiltAxis);
            }

            float biteAngle = biteMaxAngle * Mathf.Clamp01(biteInfo.bobberRotation);
            Quaternion biteRotation = Quaternion.AngleAxis(biteAngle, biteInfo.angleAxis);

            // 줄에 끌린 기울임은 빠르게 기울고 천천히 회복함
            bool isTilting = Quaternion.Angle(Quaternion.identity, dargRotation) > Quaternion.Angle(Quaternion.identity, currentDragRotation);
            float dragSpeed = isTilting ? tiltSpeed : tiltRecoverSpeed;
            currentDragRotation = Quaternion.Slerp(currentDragRotation, dargRotation, dragSpeed * Time.fixedDeltaTime);

            // 입질 회전은 기존 속도를 유지함
            currentBiteRotation = Quaternion.Slerp(currentBiteRotation, biteRotation, rotationSpeed * Time.fixedDeltaTime);

            transform.rotation = currentDragRotation * currentBiteRotation;
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.angularXDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;

            Body.excludeLayers = 1 << FishingLayer.Water;
        }

        protected override void OnDetachFromLine(FishingLinePoint point)
        {
            Body.isKinematic = true;
            velocity = Vector3.zero;
            lift = 0f;
            liftVelocity = 0f;

            // 물에 떨어질 때의 회전에서 이어서 시작함
            currentDragRotation = Quaternion.identity;
            currentBiteRotation = transform.rotation;
        }

        public void SetLineForce(Vector3 force)
        {
            lineForce = force;
        }

        // 물에서 떨어져 줄에 다시 붙음
        public void BreakOut()
        {
            lineForce = Vector3.zero;
            LinePoint.Attach(this);

            // OnAttachToLine 이 제외한 물 충돌을 다시 켜서 재착수를 감지함
            Body.excludeLayers = 0;
        }

        public void SetBiteInfo(FishBiteInfo biteInfo)
        {
            this.biteInfo = biteInfo;
        }


        // 이벤트
        protected override void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
            RuntimeJoint.angularXMotion = param.type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            RuntimeJoint.angularZMotion = param.type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        protected override void OnCastingThrow(FishingEvent.OnCastingThrow onThrow)
        {
            isLanding = false;

            RuntimeJoint.angularXMotion = ConfigurableJointMotion.Locked;
            RuntimeJoint.angularZMotion = ConfigurableJointMotion.Locked;

            Body.excludeLayers = 0;
        }

        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            LinePoint.Attach(this);
        }

        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            LinePoint.Detach(this);
            landingTimer.Reset(landingClip.duration);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            isLanding = false;

            LinePoint.Attach(this);
        }
    }
}