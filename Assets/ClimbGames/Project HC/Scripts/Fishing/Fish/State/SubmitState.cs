using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class Fish
    {
        public class SubmitState : FishStateBase
        {
            public SubmitState(Fish fish, FishingPlayer player) : base(fish, player)
            {
            }

            protected override void OnEnter()
            {
                Player.MessageBroker.Publish(new FishingEvent.OnFishSubmit() { hookedFish = Fish.Target });
            }
        }
    }
}