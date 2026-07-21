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
        public const string BodyLayer = "Full Body";
        public const string ArmsLayer = "Arms";

        public const string CastingType = "CastingType";
        public const string Throw = "Throw";
        public const string PullOut = "PullOut";
        public const string PullOut_Catch = "PullOut_Catch";
    }

    public enum CastingType
    {
        Pitching,
        OverHead,
    }
}