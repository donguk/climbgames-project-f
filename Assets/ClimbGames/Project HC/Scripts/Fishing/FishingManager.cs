using UnityEngine;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingManager")]
    public class FishingManager : MonoSingleton<FishingManager>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private Transform rodRoot;
        [SerializeField] private Transform baitRoot;
        [SerializeField] private FishingLine fishingLine;

        private FishingPoint fishingPoint;
        private FishingRod fishingRod;
        private FishingBait fishingBait;

        public void Setup(FishingPoint fishingPoint)
        {
            this.fishingPoint = fishingPoint;
            rodRoot.position = fishingPoint.RodRoot.position;

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", rodRoot);

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", baitRoot);

            fishingBait.Setup(fishingRod);
            fishingLine.Setup(fishingRod, fishingBait);

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}