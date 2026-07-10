using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class WaitForBiteState : FishingActorStateBase
        {
            public WaitForBiteState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Bobber.OnComplateLandingAsObservable().Subscribe(_ => Fish.ChangeState(FishState.Approach)).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float dragInput = FishingInput.Instance.DragDelta.magnitude;
                float reelingInput = FishingInput.Instance.Reeling;
                float limitTension = 0.5f;

                if (reelingInput > 0f)
                {
                    // 릴 감기
                    Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime);

                    if (Actor.Line.Length < 1f)
                        Actor.RetrieveRod();
                }
                else
                {
                    if (Actor.Line.Tension > limitTension * 0.2f)
                        Actor.Line.Release(0.004f);
                }

                // 찌 컨트롤
                if (Actor.Bobber.IsFloating)
                {
                    // 회전 적용
                    Actor.Bobber.ApplyLineTension(Actor.Line.Tension > limitTension && (reelingInput > 0f || dragInput > 0f) ? 1f : 0f);

                    // 이동
                    if (reelingInput > 0f)
                    {
                        if (Actor.Line.Tension > limitTension)
                            Actor.Bobber.MovePosition(Actor.Line.Tension * Time.fixedDeltaTime);
                    }
                    else if (dragInput > 0f)
                    {
                        // 직선 길이
                        float straightLength = Vector3.Distance(Actor.Rod.PoleTip.position, Actor.Bobber.transform.position);
                        float lineLength = Actor.Line.Body.CalculateLength(Actor.Line.BobberPoint.PinPoint.currentIndex);
                        if (Actor.Line.Tension > limitTension /*&& lineLength < straightLength*/)
                            Actor.Bobber.MovePosition((straightLength - lineLength) * Time.fixedDeltaTime);
                    }
                }
            }
        }
    }
}