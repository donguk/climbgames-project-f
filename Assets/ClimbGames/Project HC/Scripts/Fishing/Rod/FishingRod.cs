using UnityEngine;

namespace ClimbGames.Project_HC
{
    public abstract class FishingRod : MonoBehaviour
    {
        [SerializeField] private GameObject model;
        [SerializeField] private Transform poleTip;

        private Rigidbody baitBody;
        private float baitMomentum;
        private Vector3 lastPoleTipPosition;
        private Vector3 poleTipVelocity;
        private Vector3 relativeBaitVelocity;

        protected GameObject Model => model;
        public Transform PoleTip => poleTip;
        public float BaitMomentum => baitMomentum;
        public Vector3 PoleTipVelocity => poleTipVelocity;
        public Vector3 RelativeBaitVelocity => relativeBaitVelocity;

        void Start()
        {
            lastPoleTipPosition = PoleTip.position;
        }

        void FixedUpdate()
        {
            if (baitBody == null)
                return;

            // poleTip velocity
            poleTipVelocity = (PoleTip.position - lastPoleTipPosition) / Time.fixedDeltaTime;
            lastPoleTipPosition = PoleTip.position;

            // bait momentum
            relativeBaitVelocity = baitBody.linearVelocity - poleTipVelocity;
            Vector3 forwardDir = transform.forward;
            baitMomentum = Vector3.Dot(relativeBaitVelocity, forwardDir);
        }

        public void SetBait(FishingBait fishingBait)
        {
            baitBody = fishingBait.GetComponent<Rigidbody>();
        }

        public virtual void CastingLoad()
        {

        }

        public virtual void Swing(float intensity)
        {

        }

        public virtual void OnLoading(float intensity)
        {

        }
    }
}