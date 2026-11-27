using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishFightMachine
    {
        // 머리 흔들기: 수면 근처 제자리에서 좌우로 (장력 노이즈는 jitter 로 크게)
        public class HeadShakeState : FishFightStateBase
        {
            private const float ShakeAngle = 60f;
            private const float ShakeFrequency = 2.5f;
            private float elapsed;
            private float phase;

            protected override FishFightState State => FishFightState.HeadShake;

            public HeadShakeState(FishingPlayer player, FishFightMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnStart()
            {
                elapsed = 0f;
                phase = Random.Range(0f, Mathf.PI * 2f);
            }

            protected override Vector3 GetDesiredHeading()
            {
                elapsed += Time.fixedDeltaTime;
                float angle = Mathf.Sin(elapsed * ShakeFrequency * Mathf.PI * 2f + phase) * ShakeAngle;
                return Quaternion.AngleAxis(angle, Vector3.up) * AwayDirection;
            }

            // 정답: 감지 않음
            public override bool IsCountered() => IsReeling == false;
        }
    }
}
