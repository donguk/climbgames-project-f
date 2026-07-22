using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishStruggleInfo
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