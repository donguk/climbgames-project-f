using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 휴식: 힘을 빼고 끌려옴. 너무 세게 당기면 다시 질주
        public class RestState : FightPatternStateBase
        {
            protected override FishPatternState State => FishPatternState.Rest;
            RestClip Data => Clip as RestClip;

            public RestState(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            protected override Vector3 GetDesiredHeading() => FishForward;

            // 정답: 이때 감기
            public override bool IsCountered() => IsReeling;

            protected override void OnProgress(float t)
            {
                base.OnProgress(t);

                // 강하게 당기면 휴식을 일찍 끝냄 (다음 클립으로, 다른 타입이 없으면 휴식 유지)
                float breakOut = Data != null ? Data.breakOutTension : 0.9f;
                if (Player.Model.currentTension > breakOut)
                    Machine.BreakOut();
            }
        }
    }
}
