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

                // 줄에 들려 찌 점이 물에서 빠지면 찌를 줄에 다시 매단다
                if (Actor.Bobber.ShouldBreakOut)
                {
                    Actor.Bobber.BreakOut();
                    Player.FishMachine.ChangeState(FishState.Idle);
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