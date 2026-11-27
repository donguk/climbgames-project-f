using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishFightMachine
    {
        // 측면 질주: 줄(플레이어 → 물고기) 방향에 수직으로, 플레이어를 중심으로 호를 그림
        public class SideRunState : FishFightStateBase
        {
            private const float AwayBlend = 0.2f;   // 살짝 바깥으로 벌어지며 달림
            private float side;                     // +1: 오른쪽, -1: 왼쪽 (위에서 볼 때)
            private Vector3 rodStartForward;        // 패턴 시작 때 로드 방향 (가만히 있으면 대응으로 치지 않음)

            protected override FishFightState State => FishFightState.SideRun;

            public SideRunState(FishingPlayer player, FishFightMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnStart()
            {
                // 여유가 더 긴 쪽으로
                Vector3 away = AwayDirection;
                Vector3 right = Vector3.Cross(Vector3.up, away);
                Vector3 position = Fish.transform.position;

                float rightDistance = Player.Spot.GetFreeDistance(position, right, Pattern.runLookDistance, Pattern.depth);
                float leftDistance = Player.Spot.GetFreeDistance(position, -right, Pattern.runLookDistance, Pattern.depth);
                side = rightDistance + Random.Range(-0.5f, 0.5f) >= leftDistance ? 1f : -1f;
                rodStartForward = RodForward;
            }

            Vector3 RodForward
            {
                get
                {
                    Vector3 forward = Actor.Rod.transform.forward;
                    forward.y = 0f;
                    return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.zero;
                }
            }

            protected override Vector3 GetDesiredHeading()
            {
                Vector3 away = AwayDirection;
                Vector3 right = Vector3.Cross(Vector3.up, away);
                return (right * side + away * AwayBlend).normalized;
            }

            // 정답: 패턴 시작 때보다 로드를 물고기가 달리는 반대쪽으로 틂
            // (가만히 있어도 물고기가 옆으로 가면 줄 기준 각도는 생기므로, 로드 자체의 회전으로 판정)
            public override bool IsCountered()
            {
                Vector3 rodForward = RodForward;
                if (rodForward == Vector3.zero || rodStartForward == Vector3.zero)
                    return false;

                // 시작 방향 기준 로드 회전 (오른쪽 +)
                float rodTurn = Vector3.SignedAngle(rodStartForward, rodForward, Vector3.up);
                return rodTurn * side < 0f && Mathf.Abs(rodTurn) >= Pattern.sideCounterAngle;
            }
        }
    }
}
