using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingRod : MonoBehaviour
    {
        [SerializeField] private GameObject model;
        [SerializeField] private Transform poleTip;
        [SerializeField] private Transform lineGuide;
        [SerializeField] private FishingRodBender bender;

        protected GameObject Model => model;
        public Transform PoleTip => poleTip;
        public Transform LineGuide => lineGuide;
        public FishingRodBender Bender => bender;

        // Fighting 게임 텐션: 줄이 당기는 방향(월드)과 텐션(0~1)
        public void SetLoad(Vector3 worldDirection, float tension01)
        {
            if (bender != null)
                bender.SetLoad(worldDirection, tension01);
        }

        public void ClearLoad()
        {
            if (bender != null)
                bender.ClearLoad();
        }
    }
}
