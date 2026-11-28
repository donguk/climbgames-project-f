using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 머리 흔들기: 수면 근처 제자리에서 좌우로 (장력 노이즈는 jitter 로 크게)
        public class HeadShakeState : FightPatternStateBase
        {
            private const float ShakeAngle = 60f;
            private const float ShakeFrequency = 2.5f;
            private float elapsed;
            private float phase;

            protected override FishPatternState State => FishPatternState.HeadShake;

            public HeadShakeState(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter(ClipParam param)
            {
                base.OnEnter(param);

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
