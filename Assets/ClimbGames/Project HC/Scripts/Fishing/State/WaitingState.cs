using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingPlayer
    {
        public class WaitingState : FishingPlayerStateBase
        {
            public WaitingState(FishingPlayer player, Fish fish, FishingPlayer fsm) : base(player, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기가 물었을때
                Player.MessageBroker.Receive<FishingEvent.OnFishBite>().Subscribe(_ => fsm.ChangeState(FishingPlayerState.Fighting)).AddTo(this);
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
                        fsm.ChangeState(FishingPlayerState.Retrieving);
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