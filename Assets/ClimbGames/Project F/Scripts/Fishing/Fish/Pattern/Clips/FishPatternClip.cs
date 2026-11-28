using System;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 클립 재생 시간 범위 (초), 고정이면 min = max
    [Serializable]
    public struct ClipDuration
    {
        private const float MinDuration = 0.1f;

        [Min(MinDuration)] public float min;
        [Min(MinDuration)] public float max;

        public ClipDuration(float min, float max)
        {
            this.min = min;
            this.max = max;
        }

        // min ~ max 랜덤 (뒤바뀌었거나 너무 짧으면 보정)
        public float Pick()
        {
            // 손상된 값(NaN, 무한대)이면 타이머가 끝나지 않으므로 최소 시간으로
            if (float.IsFinite(min) == false || float.IsFinite(max) == false)
                return MinDuration;

            float low = Mathf.Max(MinDuration, Mathf.Min(min, max));
            float high = Mathf.Max(low, Mathf.Max(min, max));
            return UnityEngine.Random.Range(low, high);
        }

        public ClipDuration Validated()
        {
            float low = Mathf.Max(MinDuration, Mathf.Min(min, max));
            return new ClipDuration(low, Mathf.Max(low, max));
        }
    }

    // 물고기 행동 클립 하나 (입질, 파이팅 등). FishPattern 이 시퀀스로 묶어 FishPatternStateMachine 으로 실행
    // - 키 = asset 이름, Addressables 주소 = "PatternClip/{키}.asset" (모든 타입이 한 폴더라 이름이 겹치지 않음)
    public abstract class FishPatternClip : ScriptableObject
    {
        [Tooltip("재생 시간 범위 (초), 고정이면 min = max")]
        public ClipDuration duration = new ClipDuration(1f, 2f);

        // 이 클립을 실행할 패턴 상태
        public abstract FishPatternState Type { get; }

        // Random 모드에서 고를 때의 가중치 (스태미나 0~1)
        public virtual float EvaluateWeight(float stamina) => 1f;

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            duration = duration.Validated();
        }
#endif
    }
}
