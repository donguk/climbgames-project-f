namespace ClimbGames.Project_HC
{
    public partial class FishingLineHandleController
    {
        public class HookedState : FishingLineHandleStateBase
        {
            public HookedState(FishingLineHandle bait, FishingLineHandleController controller) : base(bait, controller)
            {
            }
        }
    }
}