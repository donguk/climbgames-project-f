using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct FishBiteInfo
    {
        public float bobberHeight;
        public float bobberRotation;
        public Vector3 angleAxis;

        public void Reset()
        {
            bobberHeight = 0f;
            bobberRotation = 0f;
        }
    }
}