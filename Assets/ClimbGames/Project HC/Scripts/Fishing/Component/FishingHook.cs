using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingHook : FishingLineObject
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == FishingLayer.Water)
                Body.linearDamping = 5f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == FishingLayer.Ground)
                FishingEvent.Broker.Publish(new FishingEvent.OnHookGrounded());
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

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