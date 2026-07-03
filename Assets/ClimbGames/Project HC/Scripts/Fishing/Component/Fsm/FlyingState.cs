using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLineController
    {
        public struct FlyingStateParam : IStateParam
        {
            public Vector3 velocity;
        }

        public class FlyingState : FishingLineStateBase
        {
            public FlyingState(FishingLine manager, FishingLineController controller) : base(manager, controller)
            {
            }

            protected override void OnEnter()
            {
                //Handle.IsKinematic = false;
                //Handle.Body.linearVelocity = param.velocity;
                //Handle.Body.linearDamping = 0f;
                //Handle.Body.excludeLayers = 0;
            }
        }
    }
}