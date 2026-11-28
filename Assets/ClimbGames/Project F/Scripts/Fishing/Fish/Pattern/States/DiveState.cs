using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 잠수: 수평은 지금 방향을 유지하고 바닥 쪽으로 (깊이는 depthRange 로)
        public class DiveState : FightPatternStateBase
        {
            private Vector3 diveDirection;

            protected override FishPatternState State => FishPatternState.Dive;

            public DiveState(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter(ClipParam param)
            {
                base.OnEnter(param);

                // 지금 방향이 플레이어 쪽이면 바깥으로 꺾음
                Vector3 forward = FishForward;
                diveDirection = Vector3.Dot(forward, AwayDirection) >= 0f ? forward : Vector3.Reflect(forward, AwayDirection);
            }

            protected override Vector3 GetDesiredHeading() => diveDirection;

            // 정답: 릴을 멈춤
            public override bool IsCountered() => IsReeling == false;
        }
    }
}
