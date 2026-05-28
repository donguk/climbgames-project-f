
using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public class CastingState : FishingActorStateBase
        {
            enum AnimationState
            {
                None,
                Throw,
                Release,
            }

            private AnimationState animationState;
            private float addtionalTime;

            public CastingState(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.PlayAnimation("Throw");
                animationState = AnimationState.Throw;
                addtionalTime = 0f;
            }

            protected override void OnUpdate()
            {
                var stateInfo = Actor.Character.CurrentAnimatorStateInfo;
                if (stateInfo.shortNameHash == FishingAnimationHash.Throw)
                {
                    switch (animationState)
                    {
                        case AnimationState.Throw:
                            {
                                if (stateInfo.normalizedTime > 0.5f)
                                {
                                    Actor.Bait.Throw(Actor.Point.CastPosition, 20f);
                                    Actor.Line.SetEnvironment(new Vector3(0f, -2f), 0.5f);
                                    animationState = AnimationState.Release;
                                }
                                break;
                            }
                        case AnimationState.Release:
                            {
                                if (stateInfo.normalizedTime > 0.7f)
                                {
                                    Actor.Character.FadeLayerWeight(0.5f);
                                    animationState = AnimationState.None;
                                }
                                break;
                            }
                    }
                }
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                switch (Actor.Bait.State)
                {
                    case FishingBaitState.Flying:
                        {
                            Actor.Line.Release(Actor.Bait.Velocity.magnitude * Time.fixedDeltaTime);
                            Actor.Line.PinBait(Actor.Bait);
                            break;
                        }
                    case FishingBaitState.Floating:
                        {
                            Actor.Line.SetEnvironment(new Vector3(0f, -9.8f), 0.98f);

                            float amount = Actor.Bait.Velocity.magnitude * Time.fixedDeltaTime;
                            Actor.Line.Release(amount);
                            Actor.Line.PinBait(Actor.Bait);

                            addtionalTime += Time.fixedDeltaTime;
                            if (addtionalTime > 0.2f)
                                fsm.ChangeState(FishingActorState.Waiting);
                            break;
                        }
                }
            }
        }
    }
}