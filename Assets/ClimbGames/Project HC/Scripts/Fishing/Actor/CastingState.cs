
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
                    Actor.CastingThrow();
                }).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();


                //float dist = Vector3.Distance(Actor.LineManager.RodTip.position, Actor.LineManager.Main.Handle.transform.position);
                //if (Actor.LineManager.Length < dist)
                //    Actor.LineManager.Release(0.05f);

                switch (Actor.Line.LineState)
                {
                    case FishingLineState.Flying:
                        {
                            Vector3 v = Actor.Line.EndPoint.Body.linearVelocity;
                            v.y = Mathf.Max(0f, v.y);
                            Actor.Line.Release(v.magnitude * Time.fixedDeltaTime);



                            break;
                        }
                    case FishingLineState.Settled:
                        {
                            fsm.ChangeState(FishingActorState.Waiting);
                            break;
                        }
                }
            }
        }
    }
}