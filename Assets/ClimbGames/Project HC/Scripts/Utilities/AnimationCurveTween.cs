using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [Serializable]
    public class AnimationCurveTween : LinearTween
    {
        [SerializeField] private AnimationCurve curve;

        public override float Value => curve.Evaluate(base.Value);

        public AnimationCurveTween()
        {
        }

        public AnimationCurveTween(float duration) : base(duration)
        {
        }
    }
}