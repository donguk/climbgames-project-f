using UnityEngine;

namespace ClimbGames.Project_HC
{
    public static class FishingLayer
    {
        public static int Water = LayerMask.NameToLayer("Water");
        public static int Ground = LayerMask.NameToLayer("Ground");
    }

    public static class FishingAnimationHash
    {
        public static int Throw = Animator.StringToHash("Throw");
    }

    public enum CastingType
    {
        Pitching,
        OverHead,
    }
}