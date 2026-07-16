using UnityEngine;
using R3;
using UnityEngine.InputSystem;

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
                Actor.Bobber.OnComplateLandingAsObservable().Subscribe(_ =>
                {
                    // 물고기 행동 시작
                    Fish.ChangeState(FishState.Approach);
                }).AddTo(this);
            }

            //protected override void OnUpdate()
            //{
            //    // 테스트
            //    if (Keyboard.current.bKey.wasReleasedThisFrame)
            //        Fish.ChangeState(FishState.Nibble);
            //}

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingInput = FishingInput.Instance.Reeling;
                float dragInput = FishingInput.Instance.DragDelta.magnitude;

                if (reelingInput > 0f)
                {
                    // 릴 감기
                    if (Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime) < 1f)
                        Actor.RetrieveRod();
                }

                LineTensionInfo tensionInfo = LineTensionInfo.Default;
                if (Actor.Bobber.IsFloating)
                {
                    tensionInfo = Actor.Line.GetTensionInfo(Actor.Bobber.transform);
                    Actor.Bobber.SetTensionInfo(tensionInfo);
                }

                // 인풋이 없는 경우
                if (reelingInput == 0f && dragInput <= 0f)
                {
                    if (tensionInfo.PullDistance < 0.001f && tensionInfo.lineLength < tensionInfo.IdleSlackLength)
                        Actor.Line.Release(0.001f);
                }

                if (Fish.IsHooked)
                    fsm.ChangeState(FishingActorState.Fighting);
            }
        }
    }
}