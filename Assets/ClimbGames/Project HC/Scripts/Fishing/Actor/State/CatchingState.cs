
using System;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class CatchingState : FishingActorStateBase
        {
            public CatchingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 애니메이션 플레이.
                Actor.Character.SetTrigger(FishingAnimation.PullOut_Catch);



            }
        }
    }
}