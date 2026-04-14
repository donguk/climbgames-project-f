using UnityEngine;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingManager")]
    public class FishingManager : MonoSingleton<FishingManager>
    {
        private FishingRod fishingRod;

        public void Setup(FishingPoint fishingPoint)
        {
            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("FishingRod_Sample", transform);

            fishingRod.gameObject.SetActive(true);
            fishingRod.Setup(fishingPoint);
        }

        public void Close()
        {
            if (fishingRod == null)
                fishingRod.gameObject.SetActive(false);
        }
    }
}