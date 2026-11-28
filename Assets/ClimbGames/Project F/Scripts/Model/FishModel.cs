using R3;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 지금 물고기 한 마리의 메모리 데이터 (FishingPlayer 가 하나를 만들어 재사용)
    // 흐름: Initialize(id) → Begin(바늘 물었을 때) → End(회수·도망)
    // HookedFish 와 FishPatternStateMachine 은 이 데이터로 움직임 (클립 asset 은 FishPattern 이 가짐)
    // - 바뀔 때만 반응하면 되는 값(물고기 유무, 파이팅 상태)은 ReactiveProperty
    // - 매 프레임 바뀌는 값(스태미나)은 plain: UI 는 Update 에서 읽음
    public class FishModel
    {
        private const float PullExponent = 1.5f;    // 지치면 속도보다 버티는 힘이 더 빨리 줄어듦

        private FishSpec spec;
        private float stamina;                  // 0~1
        private float noiseSeed;

        private ReactiveProperty<bool> isHooked = new ReactiveProperty<bool>();
        private ReactiveProperty<FishPatternState> patternType = new ReactiveProperty<FishPatternState>();

        public FishSpec Spec => spec;
        public float Stamina => stamina;
        public ReadOnlyReactiveProperty<bool> IsHooked => isHooked;
        public ReadOnlyReactiveProperty<FishPatternState> PatternType => patternType;

        // 스태미나에 따른 속도 배율 (minPowerScale ~ 1, 지구력 타입별 모양)
        public float SpeedScale
        {
            get
            {
                if (spec == null)
                    return 1f;

                float f = EvaluateEndurance(spec.endurance, stamina);
                return Mathf.Lerp(Mathf.Clamp01(spec.minPowerScale), 1f, f);
            }
        }

        // 스태미나에 따른 당기는 힘 배율
        public float PullScale => Mathf.Pow(SpeedScale, PullExponent);

        // 물고기 결정: 기본 데이터를 채움 (엑셀이 생기면 표에서 spec 을 받아 오게만 바꿈)
        public void Initialize(int fishId)
        {
            spec = CreateTempSpec(fishId);
            stamina = 1f;
            noiseSeed = Random.Range(0f, 1000f);
            isHooked.Value = false;
        }

        // 바늘을 물었을 때 (PatternType 은 FishPatternStateMachine 의 상태 전환에서만 씀)
        public void Begin()
        {
            stamina = 1f;
            isHooked.Value = true;
        }

        // 회수·도망
        public void End()
        {
            isHooked.Value = false;
        }

        public void SetPatternType(FishPatternState state)
        {
            patternType.Value = state;
        }

        // 표 값이 NaN 이면 스태미나가 영구히 NaN 이 되므로 무시
        public void DecreaseStamina(float value)
        {
            if (float.IsFinite(value))
                stamina = Mathf.Clamp01(stamina - value);
        }

        public void RecoverStamina(float value)
        {
            if (float.IsFinite(value))
                stamina = Mathf.Clamp01(stamina + value);
        }

        // 장력 노이즈 (-intensity ~ +intensity, 지치면 약해짐), 패턴별 jitter 는 호출하는 쪽에서 곱함
        public float GetTensionNoise(float time)
        {
            if (spec == null)
                return 0f;

            float currentJitter = Mathf.Max(0.01f, spec.tensionJitterIntensity * SpeedScale);
            float noise = (Mathf.PerlinNoise(time * spec.tensionJitterFrequency + noiseSeed, 0f) * 2f) - 1f;
            return noise * currentJitter;
        }

        // 지구력 타입별 힘 곡선 f(stamina), 0~1
        static float EvaluateEndurance(FishEnduranceType type, float stamina)
        {
            float s = Mathf.Clamp01(stamina);
            switch (type)
            {
                case FishEnduranceType.Stubborn:
                    {
                        float t = 1f - s;
                        return 1f - t * t * t;
                    }
                case FishEnduranceType.Sprinter:
                    return s * s * s;
                default:
                    return s;
            }
        }

        // 임시 물고기 데이터 (엑셀 전까지, 지금은 기본 1종)
        static FishSpec CreateTempSpec(int fishId)
        {
            return new FishSpec()
            {
                id = 0,
                name = "기본",
                staminaDuration = 20f,
                endurance = FishEnduranceType.Steady,
                minPowerScale = 0.2f,
                speedScale = 1f,
                pullScale = 1f,
                counteredMultiplier = 1.5f,
                uncounteredMultiplier = 0.3f,
                restRecover = 0.5f,
                tensionJitterIntensity = 0.05f,
                tensionJitterFrequency = 10f,
                verticalSpeed = 1f,
                maxDepth = 3f,
                bottomMargin = 0.2f,
                surfaceMargin = 0.1f,
                minSwimDepth = 0.3f,
                biteClips = new string[] { "Bite" },
                biteChance = 0.5f,
                fightClips = new string[] { "Run", "SideRun", "Dive", "HeadShake", "Rest" },
                fightEndMode = FishPatternEndMode.Loop,
            };
        }
    }
}
