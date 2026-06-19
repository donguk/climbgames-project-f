using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLineHandleController
    {
        public struct FlyingStateParam : IStateParam
        {
            public Vector3 position;
            public float angle;
        }

        public class FlyingState : FishingLineHandleStateBase
        {
            public FlyingState(FishingLineHandle bait, FishingLineHandleController controller) : base(bait, controller)
            {
            }
        }
    }
}