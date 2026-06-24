using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class WaitingState : FishingActorStateBase
        {
            public WaitingState(FishingActor actor, FishingActorController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                Actor.Line.Reeling(FishingModel.Instance.ReelingValue * Time.fixedDeltaTime);
                if (Actor.Line.Tension > 0.2f)
                    Actor.Line.Handle.Drag(Actor.Line.Tension);

                if (Actor.Line.Length < 1f)
                    Actor.RetrieveRod();
            }
        }
    }
}