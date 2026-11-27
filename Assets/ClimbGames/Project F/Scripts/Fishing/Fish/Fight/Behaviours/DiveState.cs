using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishFightMachine
    {
        // 잠수: 수평은 지금 방향을 유지하고 바닥 쪽으로 (깊이는 depthRange 로)
        public class DiveState : FishFightStateBase
        {
            private Vector3 diveDirection;

            protected override FishFightState State => FishFightState.Dive;

            public DiveState(FishingPlayer player, FishFightMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnStart()
            {
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
