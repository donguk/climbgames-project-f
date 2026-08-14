using System;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    [CreateAssetMenu(menuName = "Project HC/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Serializable]
        public class BiteClip
        {
            public AnimationCurve bobberHeight;
            public AnimationCurve bobberRotation;
            public float duration;
        }

        public BiteClip nibbleClip;
        public BiteClip biteClip;
        public float moveRange = 1f;
        public float moveSpeed = 3f;
        [Range(0f, 1f)]
        public float tensionSensitivity = 0.5f;
        public float StretchThreshold => Mathf.Lerp(2f, 0.2f/*최대 릴링 길이보다는 커야함*/, tensionSensitivity);
        [Range(0f, 0.2f)]
        public float tensionJitterIntensity = 0.05f;
        public float tensionJitterFrequency = 10f;
        [Range(0f, 1f)]
        public float tensionThreshold = 0.9f;
        public AnimationCurve tensionCurve;
        public float staminaDuration = 20f;
        public AnimationCurve staminaCurve;
    }
}