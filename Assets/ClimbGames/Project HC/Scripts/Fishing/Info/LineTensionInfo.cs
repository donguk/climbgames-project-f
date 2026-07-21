using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct LineTensionInfo
    {
        // 라인의 텐션
        public float lineTension;

        // 물고기가 유지하려는 텐션
        public float targetTension;

        // 최대 늘어남
        public float stretchThreshold;
        public Vector3 direction;

        // 당겨지는 경우 릴링 옵션
        public float reelPower;

        public readonly float TensionDistance => (lineTension - targetTension) * stretchThreshold;
        public readonly float TensionSpeed
        {
            get
            {
                if (lineTension > targetTension)
                    return TensionDistance * Mathf.Max(0.5f, reelPower);

                return TensionDistance * 6f;
            }
        }

        public void Reset()
        {
            lineTension = 0f;
            targetTension = 0f;
        }
    }
}