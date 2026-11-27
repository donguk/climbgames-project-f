using UnityEngine;

namespace ClimbGames.ProjectF
{
    public struct WaterSegmentInfo
    {
        private static WaterSegmentInfo _default = new WaterSegmentInfo();
        public static WaterSegmentInfo Default => _default;

        public Vector3 startPos;
        public Vector3 endPos;
        public float length;

        public readonly bool IsValid => length > 0f;
    }
}