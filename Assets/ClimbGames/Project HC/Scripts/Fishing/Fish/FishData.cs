using System;
using UnityEngine;

namespace ClimbGames.Project_HC
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

        public float lifeTime = 5f;
        public float struggleRange = 1f;
        public float moveSpeed = 2f;
    }
}