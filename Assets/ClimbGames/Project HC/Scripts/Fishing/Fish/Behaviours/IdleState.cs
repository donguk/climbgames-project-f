using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishMachine
    {
        public class IdleState : FishStateBase
        {
            public IdleState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 삭제
                Player.DeleteHookedFish();

                // 물고기가 bite 가능한 경우 접근
                Player.MessageBroker.Receive<FishingEvent.OnHookGrounded>().Subscribe(_ => fsm.ChangeState(FishState.Approach)).AddTo(this);
                Player.MessageBroker.Receive<FishingEvent.OnBobberLandedWater>().Subscribe(_ => fsm.ChangeState(FishState.Approach)).AddTo(this);

                // 이미 bite 가능한 상태일 경우
                if (Actor.Bobber.IsBitable)
                    fsm.ChangeState(FishState.Approach);
            }
        }
    }
}