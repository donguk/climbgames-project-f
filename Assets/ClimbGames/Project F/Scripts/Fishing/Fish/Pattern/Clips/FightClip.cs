using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 파이팅 클립 (타입별 하위 클래스). 여러 물고기가 같은 asset 을 같이 쓰고, 물고기별 차이는 FishSpec 의 배율(speedScale, pullScale)로 줌
    // 이름: 기본은 타입 이름 그대로(Run), 변형은 이름을 덧붙임(Run_Long)
    public abstract class FightClip : FishPatternClip
    {
        [Tooltip("헤엄 속도 (m/s, 물고기 speedScale 을 곱함)")]
        [Min(0f)] public float speed = 1f;
        [Tooltip("버티는 힘: 똑바로 도망칠 때 유지되는 장력 (0~1, 물고기 pullScale 을 곱함)")]
        [Range(0.05f, 1f)] public float pullForce = 0.5f;
        [Tooltip("장력 노이즈 배율")]
        [Range(0f, 10f)] public float jitter = 1f;
        [Tooltip("스태미나 소모 배율")]
        [Range(0f, 5f)] public float staminaCost = 1f;
        [Tooltip("목표 깊이 비율 범위 (0 = 수면 바로 아래, 1 = 바닥 / 최대 깊이)")]
        public Vector2 depthRange = new Vector2(0.2f, 0.5f);
        [Tooltip("Random 모드 선택 가중치 (x = 1 - 스태미나)")]
        public AnimationCurve weightByStamina = AnimationCurve.Constant(0f, 1f, 1f);

        // 스태미나(0~1)에 따른 가중치 (음수는 0)
        public override float EvaluateWeight(float stamina)
        {
            return weightByStamina != null ? Mathf.Max(0f, weightByStamina.Evaluate(1f - stamina)) : 1f;
        }

#if UNITY_EDITOR
        // 깊이 비율 0~1, x <= y
        protected override void OnValidate()
        {
            base.OnValidate();

            float depthMin = Mathf.Clamp01(Mathf.Min(depthRange.x, depthRange.y));
            depthRange = new Vector2(depthMin, Mathf.Clamp(depthRange.y, depthMin, 1f));
        }
#endif
    }
}
