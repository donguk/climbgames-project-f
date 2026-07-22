using System;
using R3;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingHook : FishingEquipment
    {
        private Subject<Unit> onGround = new Subject<Unit>();

        public Observable<Unit> OnGroundAsObservable() => onGround.AsObservable();

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == FishingLayer.Water)
                Body.linearDamping = 5f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == FishingLayer.Ground)
                onGround.OnNext(Unit.Default);
        }

        public override void AttachToLine(FishingLinePoint point)
        {
            base.AttachToLine(point);

            Joint.angularXMotion = ConfigurableJointMotion.Locked;
            Joint.angularZMotion = ConfigurableJointMotion.Locked;

            Body.excludeLayers |= 1 << FishingLayer.Ground;
        }

        public override void Throw(Vector3 velocity)
        {
            base.Throw(velocity);

            Body.excludeLayers &= ~(1 << FishingLayer.Ground);
        }
    }
}