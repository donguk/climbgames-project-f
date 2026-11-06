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

        [SerializeField] private float mass = 1f;
        [SerializeField] private float waterDrag = 2f;
        [SerializeField] private float staticForce = 0f;

        // 힘을 기울기 비율로 바꾸는 배율 (힘 x 배율 이 1 이면 줄 방향까지 완전히 기움)
        [SerializeField] private float tiltScale = 1f;
        [SerializeField] private float tiltSpeed = 10f;
        [SerializeField] private float tiltRecoverSpeed = 0.5f;

        [Header("Break Out")]
        [SerializeField] private float liftStiffness = 40f;
        [SerializeField] private float liftDamping = 8f;
        [SerializeField] private float breakoutHeight = 0.25f;
        // 줄이 눕혀져 있을수록 위로 당기는 힘을 적게 받음
        [SerializeField] private float liftAngleLow = 20f;
        [SerializeField] private float liftAngleHigh = 70f;

        [Header("Landing")]
        [SerializeField] private float landHeightOffset = 0.02f;

        [Header("Model")]
        [SerializeField] private float modelAlignSpeed = 8f;

        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float biteMaxAngle = 90f;

        private float waterHeight;
        private Vector3 pullVelocity;
        private Vector3 lineForce;
        private Vector3 lineDirection;
        private bool isLanding;
        private float lift;
        private float liftVelocity;
        private float lineAngle;
        private Vector3 lineVelocity;
        private Quaternion modelRestRotation = Quaternion.identity;
        private Quaternion currentDragRotation = Quaternion.identity;
        private Quaternion currentBiteRotation = Quaternion.identity;
        private NormalizedTimer landingTimer = new NormalizedTimer();
        private NormalizedTimer floatingTimer = new NormalizedTimer();
        private FishBiteInfo biteInfo;

        public bool IsFloating => RuntimeJoint == null;

        // 착수 연출이 끝난 뒤에만 분리 가능
        public bool ShouldBreakOut => IsFloating && /*landingTimer.IsCompleted &&*/ lift > breakoutHeight;

        void Awake()
        {
            if (modelRoot != null)
                modelRestRotation = modelRoot.localRotation;
        }

        void Update()
        {
            if (modelRoot != null)
                modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation, modelRestRotation, modelAlignSpeed * Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (IsFloating)
            {
                SimulatePull(Time.fixedDeltaTime);
                SimulateRotation();
                SimulateHeight();

                lineForce = Vector3.zero;
                lineVelocity = Vector3.zero;
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
            Debug.Log("[FishingBobber] LandOnWater!!!");
            landingTimer.Reset(landingClip.duration);

            LinePoint.Detach(this);
            Body.excludeLayers = 1 << FishingLayer.Water;

            MessageBroker.Publish(new FishingEvent.OnBobberEnterWater() { waterHeight = waterHeight });
        }

        void SimulateHeight()
        {
            float targetHeight = waterHeight;
            //if (landingTimer.IsCompleted == false)
            //{
            //    landingTimer.Update(Time.deltaTime);
            //    targetHeight += landingClip.height.Evaluate(landingTimer.Value);
            //
            //    if (landingTimer.IsCompleted)
            //    {
            //        MessageBroker.Publish(new FishingEvent.OnBobberLandedWater());
            //        floatingTimer.Reset(floatingClip.duration);
            //    }
            //}
            //else
            //{
            //    floatingTimer.Update(Time.deltaTime);
            //    targetHeight += floatingClip.height.Evaluate(floatingTimer.Value);
            //
            //    if (floatingTimer.IsCompleted)
            //        floatingTimer.Reset();
            //
            //    targetHeight += biteInfo.bobberHeight;
            //}

            // 연출 높이
            Vector3 position = transform.position;
            float baseHeight = Mathf.Lerp(position.y - lift, targetHeight, Time.deltaTime * 4f);

            // 줄이 위로 당기는 힘과 수면으로 되돌리는 힘으로 정해지는 높이
            float deltaTime = Time.fixedDeltaTime;
            float angleScale = Mathf.Lerp(0f, 1f, Mathf.InverseLerp(liftAngleLow, liftAngleHigh, lineAngle));
            float liftForce = lineForce.y * angleScale;

            liftVelocity += (liftForce - liftStiffness * lift) / mass * deltaTime;
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

        void SimulatePull(float deltaTime)
        {
            pullVelocity += GetHorizontalForce() / mass * deltaTime;
            pullVelocity /= 1f + waterDrag * deltaTime;
            transform.position += pullVelocity * deltaTime;
        }

        void SimulateRotation()
        {
            Quaternion dargRotation = Quaternion.identity;

            // 장력은 정지 마찰을 넘어야 기울이고, 줄의 흔들림은 찌를 그 속도로 끄는 데 필요한 힘으로 봄
            // 찌와 함께 움직이는 줄은 흔들림이 아니므로 찌의 속도를 뺌
            //float wobbleForce = (lineVelocity - pullVelocity).magnitude * mass * waterDrag;
            float tiltForce = GetHorizontalForce().magnitude;// + wobbleForce;

            // 줄 방향이 없으면 (고정점이 없을 때) 기울이지 않음
            if (tiltForce > 0.0001f && lineDirection.sqrMagnitude > 0.0001f)
            {
                // 찌의 up 에서 줄 방향까지 힘에 비례해서 기울임
                Vector3 targetUp = Vector3.Slerp(Vector3.up, lineDirection, Mathf.Clamp01(tiltForce * tiltScale));
                dargRotation = Quaternion.FromToRotation(Vector3.up, targetUp);
            }

            // 줄에 끌린 기울임은 빠르게 기울고 천천히 회복함
            bool isTilting = Quaternion.Angle(Quaternion.identity, dargRotation) > Quaternion.Angle(Quaternion.identity, currentDragRotation);
            float dragSpeed = isTilting ? tiltSpeed : tiltRecoverSpeed;
            currentDragRotation = Quaternion.Slerp(currentDragRotation, dargRotation, dragSpeed * Time.fixedDeltaTime);
            transform.rotation = currentDragRotation;
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
            pullVelocity = Vector3.zero;
            lift = 0f;
            liftVelocity = 0f;

            // 물에 떨어질 때의 회전에서 이어서 시작함
            currentDragRotation = Quaternion.identity;
            currentBiteRotation = transform.rotation;
        }

        public void SetLineState(Vector3 force, Vector3 direction, float angle, Vector3 velocity)
        {
            lineForce = force;
            lineDirection = direction;
            lineAngle = angle;
            lineVelocity = velocity;
        }

        // 조인트는 연결 순간의 회전을 기준으로 삼으므로 본체는 바로 줄 방향에 맞추고, 모델은 이전 회전에서 서서히 돌아감
        void AttachToLine()
        {
            Quaternion previousRotation = transform.rotation;
            LinePoint.Attach(this);

            if (modelRoot != null)
                modelRoot.rotation = previousRotation * modelRestRotation;
        }

        // 물에서 떨어져 줄에 다시 붙음
        public void BreakOut()
        {
            lineForce = Vector3.zero;
            lineVelocity = Vector3.zero;
            AttachToLine();

            // OnAttachToLine 이 제외한 물 충돌을 다시 켜서 재착수를 감지함
            Body.excludeLayers = 0;

            Debug.Log("[FishingBobber] BreakOut");
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
            AttachToLine();
        }

        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            LinePoint.Detach(this);
            landingTimer.Reset(landingClip.duration);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            isLanding = false;

            AttachToLine();
        }
    }
}