using UnityEngine;
using R3;
using System;

namespace ClimbGames.Project_HC
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

        [SerializeField] private float pullAcceleration = 4f;
        [Range(0f, 1f)]
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float pullMaxAngle = 80f;
        [SerializeField] private float biteMaxAngle = 90f;

        private float waterHeight;
        private Vector3 currentVelocity;
        private NormalizedTimer landingTimer = new NormalizedTimer();
        private NormalizedTimer floatingTimer = new NormalizedTimer();

        private ReactiveProperty<bool> onCompleteLanding = new ReactiveProperty<bool>();
        private LineLengthInfo lengthInfo;
        private FishBiteInfo biteInfo;

        public bool IsFloating => RuntimeJoint == null;
        public Observable<bool> OnComplateLandingAsObservable() => onCompleteLanding.AsObservable();

        void FixedUpdate()
        {
            if (IsFloating)
            {
                SimulatePull();
                SimulateRotation();
                SimulateHeight();

                lengthInfo.Reset();
                biteInfo.Reset();
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[FishingBobber] OnTggerEnter: {other.gameObject.name}");
            if (other.gameObject.layer == FishingLayer.Water)
            {
                waterHeight = other.transform.position.y;
                landingTimer.Reset(landingClip.duration);

                LinePoint.Detach(this);
                Body.excludeLayers = 1 << FishingLayer.Water;

                FishingEvent.Broker.Publish(new FishingEvent.OnBobberEnterWater());
            }
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
                    FishingEvent.Broker.Publish(new FishingEvent.OnBobberLandedWater());
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

            Vector3 position = transform.position;
            position.y = Mathf.Lerp(position.y, targetHeight, Time.deltaTime * 4f);
            transform.position = position;
        }

        void SimulatePull()
        {
            Vector3 lineDir = lengthInfo.direction;
            lineDir.y = 0f;

            if (lineDir.sqrMagnitude > 0.0001f)
            {
                Vector3 targetVelocity = lineDir.normalized * lengthInfo.pullSpeed;
                currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, pullAcceleration * Time.fixedDeltaTime);
                transform.position += targetVelocity * Time.fixedDeltaTime;
            }
        }

        void SimulateRotation()
        {
            Quaternion dargRotation = Quaternion.identity;
            Vector3 lineDir = lengthInfo.direction;
            lineDir.y = 0f;

            if (lineDir.sqrMagnitude > 0.0001f)
            {
                lineDir.Normalize();
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, lineDir);
                float lengthThreshold = Mathf.Lerp(0.2f, 0.05f, sensitivity);

                float dragAngle = Mathf.Lerp(0f, pullMaxAngle, lengthInfo.pullLength / lengthThreshold);
                dargRotation = Quaternion.AngleAxis(dragAngle, tiltAxis);
            }

            float biteAngle = biteMaxAngle * Mathf.Clamp01(biteInfo.bobberRotation);
            Quaternion biteRotation = Quaternion.AngleAxis(biteAngle, biteInfo.angleAxis);

            Quaternion targetRotation = dargRotation * biteRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.lowAngularXLimit = new SoftJointLimit() { limit = -30f };
            joint.highAngularXLimit = new SoftJointLimit() { limit = 30f };
            joint.angularZLimit = new SoftJointLimit() { limit = 30 };
            joint.angularXDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };

            Body.excludeLayers = 1 << FishingLayer.Water;
        }

        protected override void OnDetachFromLine(FishingLinePoint point)
        {
            Body.isKinematic = true;
        }

        public void SetCastingType(CastingType type)
        {
            RuntimeJoint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            RuntimeJoint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        public void SetLengthInfo(LineLengthInfo lengthInfo)
        {
            this.lengthInfo = lengthInfo;
        }

        public void SetBiteInfo(FishBiteInfo biteInfo)
        {
            this.biteInfo = biteInfo;
        }


        // 이벤트
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow onThrow)
        {
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
    }
}