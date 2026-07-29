using ClimbGames.UI;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingSpot : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private FishingWaterArea[] waterAreas;

        private UIFishingSpot uiSpot;

        public Transform SpawnPoint => spawnPoint;
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
    }
}