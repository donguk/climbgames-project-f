using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingHook : FishingLineObject
    {
        public bool IsFlying => RuntimeJoint == null && Body.isKinematic == false;

        // 컴포넌트를 처음 붙일 때 기본값: 가는 철사를 구부린 모양이라 지름은 작고 옆으로 받는 저항이 큼
        void Reset()
        {
            bodyDiameter = 0.002f;
            bodyDragScale = 2f;
            bodyDensity = 7850f;    // 강철
        }

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
            DetachFromLine();

            var body = Body;
            body.isKinematic = false;
            body.linearVelocity = param.velocity;
            body.linearDamping = 0f;
            body.excludeLayers &= ~(1 << FishingLayer.Ground);
        }

        protected override void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
            AttachToLine(LinePoint);
        }
    }
}