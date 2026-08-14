using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class FleeState : FishStateBase
        {
            public FleeState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 이벤트를 먼저 발생하여, hook/bobber나 bobber 를 원상태로 복원
                Player.MessageBroker.Publish(new FishingEvent.OnFishFlee());

                // 삭제
                Player.DeleteHookedFish();
            }

            protected override void OnUpdate()
            {
                fsm.ChangeState(FishState.Idle);
            }
        }
    }
}