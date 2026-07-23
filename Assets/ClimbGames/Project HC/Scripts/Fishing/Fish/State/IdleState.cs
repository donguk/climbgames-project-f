using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class IdleState : FishStateBase
        {
            public IdleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                FishingEvent.Broker.Receive<FishingEvent.OnBobberLandedWater>().Subscribe(_ => fsm.ChangeState(FishState.Approach)).AddTo(this);
            }
        }
    }
}