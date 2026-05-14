namespace ClimbGames.Project_HC
{
    public partial class FishingBaitStateController
    {
        public class HookedState : FishingBaitStateBase
        {
            public HookedState(FishingBait bait, FishingBaitStateController controller) : base(bait, controller)
            {
            }
        }
    }
}