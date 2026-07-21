
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class RetrievingState : FishingActorStateBase
        {
            public RetrievingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.SetTrigger(FishingAnimation.PullOut);
            }
        }
    }
}