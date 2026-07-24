using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class Fish
    {
        public class FleeState : FishStateBase
        {
            public FleeState(Fish fish, FishingPlayer player) : base(fish, player)
            {
            }

            protected override void OnEnter()
            {
                // 이벤트를 먼저 발생하여, hook/bobber나 bobber 를 원상태로 복원
                Player.MessageBroker.Publish(new FishingEvent.OnFishFlee());

                // 삭제
                Fish.DeleteHookedFish();
            }

            protected override void OnUpdate()
            {
                fsm.ChangeState(FishState.Idle);
            }
        }
    }
}