using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 파이팅 클립 공통: 바늘을 문 물고기(HookedFish)의 헤엄 의도를 만듦
        public abstract class FightPatternStateBase : PatternStateBase
        {
            private const float MinRestoreSpeed = 0.5f;     // 끌려오는 정도 계산의 최소 속도 (멈춘 물고기도 끌려옴)
            private const float MinOutward = 0.3f;          // 끌려오는 정도 계산의 최소 바깥 성분
            private const float MinPull = 0.05f;
            private const float MaxPull = 0.95f;            // 물고기 배율이 커도 목표 장력이 줄 강도를 넘지 않게

            protected HookedFish Fish => Machine.fish;
            protected FightClip Fight => Clip as FightClip;
            protected float DepthRatio { get; private set; }

            public FightPatternStateBase(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter(ClipParam param)
            {
                base.OnEnter(param);

                Vector2 depthRange = Fight != null ? Fight.depthRange : new Vector2(0.2f, 0.5f);
                DepthRatio = Mathf.Clamp01(Random.Range(depthRange.x, depthRange.y));
            }

            protected override void OnProgress(float t)
            {
                if (Fish == null)
                    return;

                Fish.SetIntent(BuildIntent());
            }

            // 패턴이 원하는 수평 방향 (수역 조향 전)
            protected abstract Vector3 GetDesiredHeading();

            // 플레이어가 이 패턴에 맞게 대응하고 있는지
            public abstract bool IsCountered();

            FishIntentInfo BuildIntent()
            {
                FishSpec spec = Spec;
                FishDepthSetting depth = spec.Depth;
                FightClip clip = Fight;

                // 표 값(배율)은 인스펙터 범위를 거치지 않으므로 음수를 막음
                float speed = (clip != null ? clip.speed : 0f) * Mathf.Max(0f, spec.speedScale) * Model.SpeedScale;
                float pullForce = Mathf.Clamp((clip != null ? clip.pullForce : 0.5f) * Mathf.Max(0f, spec.pullScale) * Model.PullScale, MinPull, MaxPull);

                float lookAhead = Mathf.Max(0.5f, speed * 0.5f);
                Vector3 away = AwayDirection;
                Vector3 heading = Player.Spot.SteerInWater(Fish.transform.position, GetDesiredHeading(), lookAhead, away, depth);

                // 장력이 pullForce 가 되도록: 바깥으로 멀어지는 속도 = 줄 힘(N) · gain, 줄 힘 = pullForce · 줄 강도(N)
                // (옆으로 달릴 때도 버티도록 바깥 성분은 최소 MinOutward 로 봄)
                float outward = Mathf.Max(MinOutward, Vector3.Dot(heading, away));
                float restoreGain = Mathf.Max(MinRestoreSpeed, speed * outward) / (pullForce * Mathf.Max(0.1f, Actor.Rod.BreakForce));

                return new FishIntentInfo()
                {
                    state = State,
                    heading = heading,
                    swimSpeed = speed,
                    depthRatio = DepthRatio,
                    pullForce = pullForce,
                    restoreGain = restoreGain,
                    jitter = clip != null ? clip.jitter : 1f,
                };
            }

            // 도움 함수

            protected bool IsReeling => Player.Model.reelingValue > 0f;

            // 플레이어(캐릭터) → 물고기 수평 방향
            protected Vector3 AwayDirection
            {
                get
                {
                    Vector3 away = Fish.transform.position - Actor.transform.position;
                    away.y = 0f;
                    return away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;
                }
            }

            protected Vector3 FishForward
            {
                get
                {
                    Vector3 forward = Fish.transform.forward;
                    forward.y = 0f;
                    return forward.sqrMagnitude > 0.0001f ? forward.normalized : AwayDirection;
                }
            }

            // away 기준 ±angle 범위에서 수역 여유가 가장 긴 방향
            protected Vector3 FindOpenDirection(Vector3 baseDirection, float angle, float lookDistance, int candidates)
            {
                Vector3 position = Fish.transform.position;
                FishDepthSetting depth = Spec.Depth;
                Vector3 best = baseDirection;
                float bestScore = float.MinValue;

                candidates = Mathf.Max(1, candidates);
                for (int i = 0; i < candidates; ++i)
                {
                    float t = candidates > 1 ? (float)i / (candidates - 1) : 0.5f;
                    Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-angle, angle, t), Vector3.up) * baseDirection;

                    // 약간의 랜덤으로 매번 같은 방향이 되지 않게
                    float score = Player.Spot.GetFreeDistance(position, direction, lookDistance, depth) + Random.Range(0f, 0.5f);
                    if (score > bestScore)
                    {
                        best = direction;
                        bestScore = score;
                    }
                }

                return best;
            }
        }
    }
}
