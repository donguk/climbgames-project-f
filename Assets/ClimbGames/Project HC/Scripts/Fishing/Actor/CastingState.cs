
using System;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class CastingState : FishingActorStateBase
        {
            public CastingState(FishingActor actor, FishingActorController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                var animator = Actor.Character.PlayAnimation(FishingAnimation.CastingThrow);
                int layerIndex = animator.GetLayerIndex(FishingAnimation.LayerName);

                animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.CastingThrow, 0.4f, layerIndex).Subscribe(_ =>
                {
                    CastingType castingType = FishingModel.Instance.CastingType.CurrentValue;
                    float angle = castingType == CastingType.OverHead ? 45f : 20f;

                    Actor.Hook.CastingThrow();
                    Actor.Line.Handle.CastingThrow(Actor.Area.CastPosition, angle);
                }).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                switch (Actor.Line.Handle.State)
                {
                    case FishingLineHandleState.Flying:
                        {
                            Actor.Line.Release(Actor.Line.Handle.Body.linearVelocity.magnitude * Time.fixedDeltaTime);
                            break;
                        }
                    case FishingLineHandleState.Floating:
                        {
                            //Actor.Line.SetEnvironment(new Vector3(0f, -9.8f), 0.98f);
                            //
                            //float amount = Actor.Bait.Velocity.magnitude * Time.fixedDeltaTime;
                            //Actor.Line.Release(amount);
                            //Actor.Line.PinBait(Actor.Bait);
                            //
                            //addtionalTime += Time.fixedDeltaTime;
                            //if (addtionalTime > 0.2f)
                            //    fsm.ChangeState(FishingActorState.Waiting);
                            break;
                        }
                }
            }
        }
    }
}