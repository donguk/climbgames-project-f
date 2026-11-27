using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishFightMachine
    {
        // 질주: 플레이어 반대쪽 중 수역 여유가 가장 긴 방향으로
        public class RunState : FishFightStateBase
        {
            private const int Candidates = 7;
            private Vector3 runDirection;

            protected override FishFightState State => FishFightState.Run;

            public RunState(FishingPlayer player, FishFightMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnStart()
            {
                runDirection = FindOpenDirection(AwayDirection, Pattern.runAngle, Candidates);
            }

            protected override Vector3 GetDesiredHeading() => runDirection;

            // 정답: 릴을 멈춤
            public override bool IsCountered() => IsReeling == false;
        }
    }
}
