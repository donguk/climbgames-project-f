using System.Runtime.CompilerServices;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public class IdleState : FishingActorStateBase
        {
            public IdleState(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.PlayAnimation("Idle", 0.7f);
            }
        }
    }
}