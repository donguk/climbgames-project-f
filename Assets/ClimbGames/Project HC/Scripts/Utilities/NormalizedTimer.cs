using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [Serializable]
    public class NormalizedTimer
    {
        protected float duration;
        private float elapsedTime;

        public virtual float Value => duration > 0f ? Mathf.Clamp01(elapsedTime / duration) : 0f;
        public bool IsCompleted => elapsedTime >= duration;

        public NormalizedTimer() : this(1f)
        {
        }

        public NormalizedTimer(float duration)
        {
            Reset(duration);
        }

        public void Update(float deltaTime)
        {
            if (duration <= 0f)
                return;

            if (elapsedTime < duration)
                elapsedTime += deltaTime;
        }

        public void Reset(bool reverse = false)
        {
            elapsedTime = reverse ? duration : 0f;
        }

        public void Reset(float duration)
        {
            this.duration = duration;
            Reset();
        }
    }
}