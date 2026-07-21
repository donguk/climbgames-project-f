using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishStruggleInfo
    {
        public WaterAreaInfo areaInfo;
        public float stamina;
        public float currentSpeed;

        public void Reset()
        {
            areaInfo.Reset();
            currentSpeed = 0;
            stamina = 0f;
        }
    }
}