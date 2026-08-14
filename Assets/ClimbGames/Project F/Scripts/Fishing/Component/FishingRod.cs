using UnityEngine;

namespace ClimbGames.ProjectF
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