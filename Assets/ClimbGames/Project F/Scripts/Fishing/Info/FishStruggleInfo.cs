using UnityEngine;

namespace ClimbGames.ProjectF
{
    public struct FishStruggleInfo
    {
        public WaterAreaInfo areaInfo;
        public float stamina;
        public float speed;

        public void Reset()
        {
            areaInfo.Reset();
            speed = 0;
            stamina = 0f;
        }
    }
}