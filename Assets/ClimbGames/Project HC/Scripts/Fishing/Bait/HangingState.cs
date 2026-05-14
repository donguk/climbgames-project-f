using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingBaitStateController
    {
        public class HangingState : FishingBaitStateBase
        {
            public HangingState(FishingBait bait, FishingBaitStateController controller) : base(bait, controller)
            {
            }

            protected override void OnEnter()
            {
                Bait.Body.linearVelocity = Vector3.zero;
                Bait.Body.angularVelocity = Vector3.zero;
                Bait.Body.linearDamping = 1f;
                Bait.Body.excludeLayers |= 1 << FishingLayer.Water | 1 << FishingLayer.Ground;

                Bait.Joint.xMotion = ConfigurableJointMotion.Limited;
                Bait.Joint.yMotion = ConfigurableJointMotion.Limited;
                Bait.Joint.zMotion = ConfigurableJointMotion.Limited;
            }
        }
    }
}