using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct WaterAreaInfo
    {
        public Vector3 center;
        public float range;

        public void Reset()
        {
            center = Vector3.zero;
            range = 0f;
        }
    }
}