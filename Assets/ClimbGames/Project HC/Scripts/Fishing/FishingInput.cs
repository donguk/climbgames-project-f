using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public static class FishingInput
    {
        public interface ICastAction
        {
            public float loadIntensity { get; }
            public Vector2 direction { get; }

            public event Action onPressed;
            public event Action onReleased;
            public event Action onCanceled;
        }
    }
}