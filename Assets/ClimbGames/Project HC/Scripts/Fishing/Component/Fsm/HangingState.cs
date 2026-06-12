using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLineHandleController
    {
        public class HangingState : FishingLineHandleStateBase
        {
            public HangingState(FishingLineHandle bait, FishingLineHandleController controller) : base(bait, controller)
            {
            }

            protected override void OnEnter()
            {
                Handle.Body.linearVelocity = Vector3.zero;
                Handle.Body.angularVelocity = Vector3.zero;
                Handle.Body.linearDamping = 1f;
                Handle.Body.excludeLayers |= 1 << FishingLayer.Water | 1 << FishingLayer.Ground;

                //Handle.Joint.xMotion = ConfigurableJointMotion.Limited;
                //Handle.Joint.yMotion = ConfigurableJointMotion.Limited;
                //Handle.Joint.zMotion = ConfigurableJointMotion.Limited;

                //Bait.Float.Stop();
            }
        }
    }
}