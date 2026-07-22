using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishStatus
    {
        /// <summary>
        /// 스테미너
        /// </summary>
        public float stamina;          // 0~1
        /// <summary>
        /// 텐션 노이즈 시드
        /// </summary>
        public float noiseSeed;
        /// <summary>
        /// 현재 라인 텐션
        /// </summary>
        public float lineTension;
        /// <summary>
        /// 보간중인 텐션 
        /// </summary>
        public float currentTension;
        /// <summary>
        /// 임계값보다 오버시 누적되는 시간
        /// </summary>
        public float tensionOverTime;

        public void Initialize(FishData data)
        {
            stamina = 1f;
            noiseSeed = Random.Range(0f, 1000f);
            lineTension = 0f;
            currentTension = 0f;
            tensionOverTime = 0f;
        }

        public void DecreaseStamina(float value)
        {
            stamina = Mathf.Clamp01(stamina - value);
        }
    }
}