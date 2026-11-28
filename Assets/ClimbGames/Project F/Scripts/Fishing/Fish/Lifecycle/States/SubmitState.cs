using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public partial class FishLifecycleStateMachine
    {
        public class SubmitState : LifecycleStageBase
        {
            public SubmitState(FishingPlayer player, FishLifecycleStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                Player.MessageBroker.Publish(new FishingEvent.OnFishSubmit() { hookedFish = Player.Fish });
            }
        }
    }
}