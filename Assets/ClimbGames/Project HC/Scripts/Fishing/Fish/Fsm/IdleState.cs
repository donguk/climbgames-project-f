namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class IdleState : FishStateBase
        {
            public IdleState(FishingActor actor, FishController fsm) : base(actor, fsm)
            {
            }
        }
    }
}