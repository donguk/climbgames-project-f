using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingRod : MonoBehaviour
    {
        [SerializeField] private GameObject model;
        [SerializeField] private Transform poleTip;
        [SerializeField] private Transform lineGuide;

        protected GameObject Model => model;
        public Transform PoleTip => poleTip;
        public Transform LineGuide => lineGuide;
    }
}