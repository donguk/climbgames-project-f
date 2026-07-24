using R3;

namespace ClimbGames.Project_HC
{
    public partial class Fish
    {
        public class IdleState : FishStateBase
        {
            public IdleState(Fish fish, FishingPlayer player) : base(fish, player)
            {
            }

            //protected override void OnEnter()
            //{
            //    // 물고기가 bite 가능한 경우 접근
            //    FishingEvent.Broker.Receive<FishingEvent.OnHookGrounded>().Subscribe(_ => fsm.ChangeState(FishState.Approach)).AddTo(this);
            //    FishingEvent.Broker.Receive<FishingEvent.OnBobberLandedWater>().Subscribe(_ => fsm.ChangeState(FishState.Approach)).AddTo(this);
            //
            //    // 이미 bite 가능한 상태일 경우
            //    if (Actor.Bobber.IsBitable)
            //        fsm.ChangeState(FishState.Approach);
            //}
        }
    }
}