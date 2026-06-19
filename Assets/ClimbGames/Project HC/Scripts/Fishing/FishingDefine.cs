using UnityEngine;

namespace ClimbGames.Project_HC
{
    public static class FishingLayer
    {
        public static int Water = LayerMask.NameToLayer("Water");
        public static int Ground = LayerMask.NameToLayer("Ground");
    }

    public static class FishingAnimation
    {
        public const string LayerName = "Fishing Layer";
        public const string CastingThrow = "Casting_Throw";
        public static int CastingThrowHash = Animator.StringToHash("Casting_Throw");
        public static int WaitingHash = Animator.StringToHash("Waiting");
    }

    public enum CastingType
    {
        Pitching,
        OverHead,
    }
}