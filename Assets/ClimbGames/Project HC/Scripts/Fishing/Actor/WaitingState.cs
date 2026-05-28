using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public class WaitingState : FishingActorStateBase
        {
            public WaitingState(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                Actor.Line.Reeling(Actor.Input.ReelingValue * Time.fixedDeltaTime);

                float tensionIntensity = Actor.Line.CalculateTension(Actor.Bait);
                if (tensionIntensity > 0f)
                    Actor.Bait.Drag(Actor.Line.End.up, tensionIntensity);

                if (Actor.Line.Length < 1f && (Actor.Bait.LocationFlag == 0 || tensionIntensity > 0.2f))
                {
                    Debug.Log($"[FishingActor] Suspend Bait: lineLength({Actor.Line.Length})/ baitLocation({Actor.Bait.LocationFlag})/ tensionIntensity({tensionIntensity})");

                    Actor.Bait.Attach(Actor.Line.End);
                    fsm.ChangeState(FishingActorState.Idle);
                }
                else
                {
                    Actor.Line.PinBait(Actor.Bait);
                }
            }
        }
    }
}