using ClimbGames.UI;
using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public class FishingSpot : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private FishingWaterArea[] waterAreas;

        private UIFishingSpot uiSpot;

        public Transform SpawnPoint => spawnPoint;
        public FishingWaterArea[] WaterAreas => waterAreas;
        public FishingWaterArea WaterArea => waterAreas[0];

        void Awake()
        {
            uiSpot = UIManager.Instance.CreateUI<UIFishingSpot>("UIFisingSpot", UILayer.World);
            uiSpot.Setup(this);

            WorldManager.Instance.CurrentMode.Subscribe(_ => uiSpot.gameObject.SetActive(_ == WorldMode.Map)).AddTo(this);
        }

        void OnDestroy()
        {
            if (uiSpot != null)
                UIManager.HideSafely(uiSpot);
        }

        public WaterSegmentInfo GetCastingArea(Transform target)
        {
            WaterSegmentInfo targetInfo = WaterSegmentInfo.Default;
            Vector3 targetPos = target.transform.position;

            float highestScore = 0f;
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                var areaInfo = waterAreas[i].GetInnerSegmentInfo(target);
                targetPos.y = waterAreas[i].Center.y;

                // 범위가 길고, 가까운 영역 선택을 위한 스코어 계산
                float score = areaInfo.length / Vector3.Distance(targetPos, waterAreas[i].Center);
                if (highestScore < score)
                {
                    targetInfo = areaInfo;
                    highestScore = score;
                }
            }

            return targetInfo;
        }

        public WaterAreaInfo GetStruggleArea(Transform target, float range)
        {
            WaterAreaInfo targetInfo = WaterAreaInfo.Default;
            Vector3 targetPos = target.position;

            float shortestDist = float.MaxValue;
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                targetPos.y = waterAreas[i].Center.y;
                float dist = Vector3.Distance(targetPos, waterAreas[i].Center);

                // 가장 가까운 영역 선택
                if (dist < shortestDist)
                {
                    targetInfo = waterAreas[i].GetInnerAreaInfo(targetPos, range);
                    shortestDist = dist;
                }
            }

            return targetInfo;
        }
    }
}