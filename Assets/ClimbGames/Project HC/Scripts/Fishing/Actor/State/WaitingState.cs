using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class WaitingState : FishingActorStateBase
        {
            public WaitingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기가 물었을때
                FishingEvent.Broker.Receive<FishingEvent.OnFishBite>().Subscribe(_ => fsm.ChangeState(FishingActorState.Fighting)).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingInput = FishingInput.Instance.Reeling;
                float dragInput = FishingInput.Instance.DragDelta.magnitude;

                if (reelingInput > 0f)
                {
                    // 릴 감기
                    if (Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime) < 1f)
                        fsm.ChangeState(FishingActorState.Retrieving);
                }

                LineLengthInfo lengthInfo = LineLengthInfo.Default;
                if (Actor.Bobber.IsFloating)
                {
                    lengthInfo = Actor.Line.GetLengthInfo(Actor.Bobber.transform);
                    Actor.Bobber.SetLengthInfo(lengthInfo);
                }

                // 인풋이 없는 경우 약간씩 풀림
                if (reelingInput <= 0f && dragInput <= 0f)
                {
                    if (lengthInfo.IsTensioned)
                        Actor.Line.Release(0.002f);
                }
            }
        }
    }
}