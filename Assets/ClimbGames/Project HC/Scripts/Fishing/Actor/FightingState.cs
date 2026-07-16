
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class FightingState : FishingActorStateBase
        {
            public FightingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float dragInput = FishingInput.Instance.DragDelta.magnitude;
                float reelingInput = FishingInput.Instance.Reeling;

                if (reelingInput > 0f)
                {
                    // 릴 감기
                    Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime);

                    if (Actor.Line.TotalLength < 1f)
                        Actor.RetrieveRod();
                }
            }
        }
    }
}