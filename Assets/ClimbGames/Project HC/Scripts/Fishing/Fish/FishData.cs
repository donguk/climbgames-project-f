using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [CreateAssetMenu(menuName = "Project HC/Fish Data")]
    public class FishData : ScriptableObject
    {
        [Serializable]
        public class BiteMotion
        {
            [SerializeField] private WaterOffsetTween bobberTween;

            public float BobberOffset => bobberTween.Value;
            public bool IsCompleted => bobberTween.IsCompleted;

            public void Update(float deltaTime)
            {
                bobberTween.Update(deltaTime);
            }

            public void Reset()
            {
                bobberTween.Reset();
            }
        }

        public BiteMotion nibble;
        public BiteMotion bite;
    }
}