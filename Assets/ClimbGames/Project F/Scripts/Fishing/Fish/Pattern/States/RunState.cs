using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 질주: 플레이어 반대쪽 중 수역 여유가 가장 긴 방향으로
        public class RunState : FightPatternStateBase
        {
            private const int Candidates = 7;
            private Vector3 runDirection;

            protected override FishPatternState State => FishPatternState.Run;
            RunClip Data => Clip as RunClip;

            public RunState(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter(ClipParam param)
            {
                base.OnEnter(param);

                runDirection = Data != null
                    ? FindOpenDirection(AwayDirection, Data.runAngle, Data.lookDistance, Candidates)
                    : AwayDirection;
            }

            protected override Vector3 GetDesiredHeading() => runDirection;

            // 정답: 릴을 멈춤
            public override bool IsCountered() => IsReeling == false;
        }
    }
}
