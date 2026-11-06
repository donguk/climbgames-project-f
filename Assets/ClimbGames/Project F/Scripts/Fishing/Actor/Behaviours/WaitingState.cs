using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public partial class FishingActorMachine
    {
        public class WaitingState : ActorStateBase
        {
            public WaitingState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기가 물었을때
                Player.MessageBroker.Receive<FishingEvent.OnFishBite>().Subscribe(_ => fsm.ChangeState(FishingActorState.Fighting)).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingValue = Player.Model.reelingValue;
                if (reelingValue > 0f)
                {
                    // 릴 감기
                    if (Actor.Line.Reeling(reelingValue * Time.fixedDeltaTime) < 1f)
                        fsm.ChangeState(FishingActorState.Retrieving);
                }

                // 줄이 당기는 힘을 찌에 전달
                if (Actor.Bobber.IsFloating)
                {
                    Actor.Bobber.SetLineState(Actor.Line.Body.PinForce, Actor.Line.Body.PinDirection, Actor.Line.Body.PinAngle, Actor.Line.Body.PinLineVelocity);

                    // 위로 세게 당겨지면 물에서 분리
                    if (Actor.Bobber.ShouldBreakOut)
                    {
                        Actor.Bobber.BreakOut();

                        // 찌가 물에서 떨어지면 줄은 공중처럼 떨어짐 (재착수 시 다시 켜짐)
                        Actor.Line.Body.SetWaterContact(false);

                        Player.FishMachine.ChangeState(FishState.Idle);
                    }
                }

                // 인풋이 없는 경우 약간씩 풀림
                // LineLengthInfo lengthInfo = Actor.Bobber.IsFloating ? Actor.Line.GetLengthInfo(Actor.Bobber.transform) : LineLengthInfo.Default;
                // if (reelingValue <= 0f)
                // {
                //     if (lengthInfo.IsTensioned)
                //         Actor.Line.Release(0.002f);
                // }
            }
        }
    }
}