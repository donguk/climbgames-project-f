using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class SubmitState : FishStateBase
        {
            public SubmitState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                Player.MessageBroker.Publish(new FishingEvent.OnFishSubmit() { hookedFish = Player.Fish });
            }
        }
    }
}