using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLine : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;

        private FishingRod fishingRod;
        private FishingBait fishingBait;

        void Awake()
        {
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();

            lineRenderer.positionCount = 2;
        }

        public void Setup(FishingRod fishingRod, FishingBait fishingBait)
        {
            this.fishingRod = fishingRod;
            this.fishingBait = fishingBait;
        }

        void LateUpdate()
        {
            if (lineRenderer == null || fishingRod == null || fishingBait == null)
                return;

            // Line의 시작점을 낚싯대 끝으로 업데이트
            lineRenderer.SetPosition(0, fishingRod.PoleTipe.position);

            // Line의 끝점을 미끼 위치로 업데이트
            lineRenderer.SetPosition(1, fishingBait.transform.position);
        }
    }
}