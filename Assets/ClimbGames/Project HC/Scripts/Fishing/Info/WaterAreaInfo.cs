using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct WaterAreaInfo
    {
        private static WaterAreaInfo _default = new WaterAreaInfo();
        public static WaterAreaInfo Default => _default;

        public Vector3 center;
        public float range;

        public void Reset()
        {
            center = Vector3.zero;
            range = 0f;
        }
    }

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