using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
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

                LineLengthInfo lengthInfo = LineLengthInfo.Default;
                if (Actor.Bobber.IsFloating)
                {
                    lengthInfo = Actor.Line.GetLengthInfo(Actor.Bobber.transform);
                    Actor.Bobber.SetLengthInfo(lengthInfo);
                }

                // 인풋이 없는 경우 약간씩 풀림
                if (reelingValue <= 0f)
                {
                    if (lengthInfo.IsTensioned)
                        Actor.Line.Release(0.002f);
                }
            }
        }
    }
}