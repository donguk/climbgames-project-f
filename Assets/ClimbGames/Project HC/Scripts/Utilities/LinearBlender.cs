using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class LinearBlender
    {
        public float Duration { get; set; }
        public float ElapsedTime { get; private set; }
        public float Value { get; private set; }
        public float SmoothValue => Mathf.SmoothStep(0f, 1f, Value);

        public LinearBlender(float duration)
        {
            Duration = duration;
            Reset();
        }

        public void Update(float deltaTime)
        {
            if (Duration <= 0f)
            {
                Value = 1f;
                return;
            }

            if (ElapsedTime < Duration)
            {
                ElapsedTime += deltaTime;
                Value = Mathf.Clamp01(ElapsedTime / Duration);
            }
            else
            {
                Value = 1f;
            }
        }

        public void Reset(bool reverse = false)
        {
            ElapsedTime = reverse ? Duration : 0f;
            Value = reverse ? 1f : 0f;
        }
    }
}