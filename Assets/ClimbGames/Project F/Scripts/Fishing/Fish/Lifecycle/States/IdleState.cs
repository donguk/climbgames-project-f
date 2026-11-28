using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.ProjectF
{
    public partial class FishLifecycleStateMachine
    {
        public class IdleState : LifecycleStageBase
        {
            public IdleState(FishingPlayer player, FishLifecycleStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 삭제
                Player.DeleteHookedFish();

                // 물고기가 bite 가능한 경우 접근
                Player.MessageBroker.Receive<FishingEvent.OnHookGrounded>().Subscribe(_ => fsm.ChangeState(FishLifecycleState.Approach)).AddTo(this);
                Player.MessageBroker.Receive<FishingEvent.OnBobberLandedWater>().Subscribe(_ => fsm.ChangeState(FishLifecycleState.Approach)).AddTo(this);
            }
        }
    }
}