using UnityEngine;

namespace ClimbGames.ProjectF
{
    [CreateAssetMenu(menuName = "Project HC/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Range(0f, 0.2f)]
        public float tensionJitterIntensity = 0.05f;
        public float tensionJitterFrequency = 10f;
        [Range(0f, 1f)]
        public float tensionThreshold = 0.9f;
        public float staminaDuration = 20f;
        public AnimationCurve staminaCurve;
    }
}
