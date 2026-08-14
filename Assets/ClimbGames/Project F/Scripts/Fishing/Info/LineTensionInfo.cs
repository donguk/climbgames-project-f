using UnityEngine;

namespace ClimbGames.ProjectF
{
    public struct LineTensionInfo
    {
        public float lineTension;
        public float fishTension;
        public Vector3 direction;
        public float intensity;

        public void Reset()
        {
            direction = Vector3.zero;
            intensity = 0f;
        }
    }
}