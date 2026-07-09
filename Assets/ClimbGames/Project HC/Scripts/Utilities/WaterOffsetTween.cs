using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [Serializable]
    public class WaterOffsetTween : AnimationCurveTween
    {
        [SerializeField] private float offset = 1f;

        public override float Value => base.Value * offset;
    }
}