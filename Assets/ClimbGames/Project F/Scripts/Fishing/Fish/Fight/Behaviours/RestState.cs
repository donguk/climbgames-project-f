using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishFightMachine
    {
        // 휴식: 힘을 빼고 끌려옴. 너무 세게 당기면 다시 질주
        public class RestState : FishFightStateBase
        {
            protected override FishFightState State => FishFightState.Rest;

            public RestState(FishingPlayer player, FishFightMachine fsm) : base(player, fsm)
            {
            }

            protected override Vector3 GetDesiredHeading() => FishForward;

            // 정답: 이때 감기
            public override bool IsCountered() => IsReeling;

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                // 패턴이 이미 바뀌었으면 무시
                if (Machine.CurrentState != this)
                    return;

                // 강하게 당기면 다시 도망
                if (Player.Model.currentTension > Player.FishData.tensionThreshold)
                    Machine.ChangeTo(FishFightState.Run);
            }
        }
    }
}
