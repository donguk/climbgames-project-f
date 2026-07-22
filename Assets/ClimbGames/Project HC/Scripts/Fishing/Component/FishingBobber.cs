using UnityEngine;
using R3;
using System;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : FishingEquipment
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
        private Subject<Unit> onFloating = new Subject<Unit>();
        private Subject<Unit> onCompleteLanding = new Subject<Unit>();
        private LineLengthInfo lengthInfo;
        private FishBiteInfo biteInfo;

        public bool IsLanding => landingTimer.IsCompleted == false;
        public bool IsFloating => Joint == null;
        public Observable<Unit> OnFloatingAsObservable() => onFloating.AsObservable();
        public Observable<Unit> OnComplateLandingAsObservable() => onCompleteLanding.AsObservable();

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
                if (Joint != null)
                {
                    Destroy(Joint);

                    var body = Body;
                    body.isKinematic = true;
                    body.excludeLayers = 1 << FishingLayer.Water;
                    ConnectedPoint.SetTrackingTarget(transform);

                    waterHeight = other.transform.position.y;
                    landingTimer.Reset(landingClip.duration);
                    onFloating.OnNext(Unit.Default);
                }
            }
        }

        void SimulateHeight()
        {
            float targetHeight = waterHeight;
            if (IsLanding)
            {
                landingTimer.Update(Time.deltaTime);
                targetHeight += landingClip.height.Evaluate(landingTimer.Value);

                if (landingTimer.IsCompleted)
                {
                    onCompleteLanding.OnNext(Unit.Default);
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
                Vector3 targetVelocity = lineDir.normalized * lengthInfo.PullSpeed;
                currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, pullAcceleration * Time.fixedDeltaTime);
                transform.position += targetVelocity * Time.fixedDeltaTime;
            }
        }

        void SimulateRotation()
        {
            Quaternion tensionRotation = Quaternion.identity;
            Vector3 lineDir = lengthInfo.direction;
            lineDir.y = 0f;

            if (lineDir.sqrMagnitude > 0.0001f)
            {
                lineDir.Normalize();
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, lineDir);
                float stretchThreshold = Mathf.Lerp(0.2f, 0.05f, sensitivity);

                float tensionAngle = Mathf.Lerp(0f, pullMaxAngle, lengthInfo.TensionedLength / stretchThreshold);
                tensionRotation = Quaternion.AngleAxis(tensionAngle, tiltAxis);
            }

            float biteAngle = biteMaxAngle * Mathf.Clamp01(biteInfo.bobberRotation);
            Quaternion biteRotation = Quaternion.AngleAxis(biteAngle, biteInfo.angleAxis);

            Quaternion targetRotation = tensionRotation * biteRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        public void SetCastingType(CastingType type)
        {
            Joint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            Joint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        public override void AttachToLine(FishingLinePoint point)
        {
            base.AttachToLine(point);

            var joint = Joint;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.lowAngularXLimit = new SoftJointLimit() { limit = -30f };
            joint.highAngularXLimit = new SoftJointLimit() { limit = 30f };
            joint.angularZLimit = new SoftJointLimit() { limit = 30 };
            joint.angularXDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };

            Body.excludeLayers = 1 << FishingLayer.Water;
        }

        public override void Throw(Vector3 velocity)
        {
            //

            Joint.angularXMotion = ConfigurableJointMotion.Locked;
            Joint.angularZMotion = ConfigurableJointMotion.Locked;

            Body.excludeLayers = 0;
        }

        public void SetLengthInfo(LineLengthInfo lengthInfo)
        {
            this.lengthInfo = lengthInfo;
        }

        public void SetBiteInfo(FishBiteInfo biteInfo)
        {
            this.biteInfo = biteInfo;
        }
    }
}