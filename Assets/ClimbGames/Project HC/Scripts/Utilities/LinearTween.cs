using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [Serializable]
    public class LinearTween
    {
        [SerializeField] private float duration;
        [SerializeField] private bool isLoop;
        private float elapsedTime;

        public virtual float Value => duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 0f;
        public bool IsCompleted => elapsedTime >= duration;

        public LinearTween()
        {
        }

        public LinearTween(float duration)
        {
            this.duration = duration;
            Reset();
        }

        public void Update(float deltaTime)
        {
            if (duration <= 0f)
                return;

            if (elapsedTime < duration)
                elapsedTime += deltaTime;

            if (elapsedTime >= duration && isLoop)
                elapsedTime = 0;
        }

        public void Reset(bool reverse = false)
        {
            elapsedTime = reverse ? duration : 0f;
        }
    }
}