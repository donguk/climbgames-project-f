namespace ClimbGames.Project_HC
{
    public partial class FishingLineController
    {
        public class HookedState : FishingLineStateBase
        {
            public HookedState(FishingLine manager, FishingLineController controller) : base(manager, controller)
            {
            }
        }
    }
}