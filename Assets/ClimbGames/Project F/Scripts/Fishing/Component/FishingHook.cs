using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingHook : FishingLineObject
    {
        public bool IsFlying => RuntimeJoint == null && Body.isKinematic == false;

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == FishingLayer.Water)
                Body.linearDamping = 5f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == FishingLayer.Ground)
                MessageBroker.Publish(new FishingEvent.OnHookGrounded());
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;

            Body.excludeLayers |= 1 << FishingLayer.Ground;
        }


        // 이벤트 
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            LinePoint.Detach(this);

            var body = Body;
            body.isKinematic = false;
            body.linearVelocity = param.velocity;
            body.linearDamping = 0f;
            body.excludeLayers &= ~(1 << FishingLayer.Ground);
        }

        protected override void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
            LinePoint.Attach(this);
        }
    }
}