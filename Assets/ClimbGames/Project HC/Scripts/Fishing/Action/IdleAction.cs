using System.Runtime.CompilerServices;

namespace ClimbGames.Project_HC
{
    public partial class FishingActionController
    {
        public class IdleAction : ActionBase
        {
            public IdleAction(FishingActor actor, FishingActionController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.PlayAnimation("Idle", 0.7f);
            }
        }
    }
}