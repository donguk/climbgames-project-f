using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine : VerletLine
    {
        [Header("Fishing")]
        [SerializeField] private FishingLinePoint bobberPoint;
        [SerializeField] private FishingLineHandle lineHandle;

        [SerializeField] private float totalLength;
        [Range(0f, 1f)]
        [SerializeField] private float lineTension = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float bobberHeight;

        private Transform rodTip;
        protected override Transform AnchorTransform => rodTip != null ? rodTip : base.AnchorTransform;
        public float Length => totalLength;
        public float VisualLength => lineHandle.HasForce ? Vector3.Distance(AnchorTransform.position, lineHandle.transform.position) : totalLength;

        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint HookPoint => lineHandle;

        void Start()
        {
            enableSpline = false;
        }

        public void Initialize(Transform rodTip, float length, float bobberHeight)
        {
            this.rodTip = rodTip;

            bobberPoint.Initialize(this);
            lineHandle.Initialize(this);

            BuildLine(length);
            bobberPoint.SetHeight(bobberHeight);
        }

        void BuildLine(float length)
        {
            totalLength = Mathf.Max(0.1f, length);

            ResizePositionArray(positionCount);
            UpdateSegment();

            Vector3 position = AnchorTransform.position;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                position.y -= segmentLength;
            }
        }

        public void UpdateSegment()
        {
            segmentLength = VisualLength / (positionCount - 1);
        }

        void UpdatePoints()
        {
            UpdateSegment();

            bobberPoint.UpdatePosition();
            lineHandle.UpdatePosition();
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
        }

        public void SetHandleTarget(Transform target)
        {
            lineHandle.SetTarget(target);
            UpdateSegment();
        }

        //public void PinBait(FishingBait bait)
        //{
        //    SetPosition(positionCount - 1, bait.transform.position);
        //    bait.transform.up = hookSocket.transform.up;
        //}

        public void Release(float amount)
        {
            if (amount > 0f)
            {
                totalLength += amount;
                segmentLength = totalLength / (positionCount - 1);
            }
        }

        public void Reeling(float amount)
        {
            if (amount > 0f)
            {
                totalLength = Mathf.Max(0.1f, totalLength - amount);
                segmentLength = totalLength / (positionCount - 1);
            }
        }

        public float CalculateTension(FishingLineHandle bait)
        {
            float distance = Vector3.Distance(rodTip.position, bait.transform.position);
            float straightSegmentLength = distance / (positionCount - 1);
            float threshold = straightSegmentLength * 0.92f;

            return Mathf.Clamp01((threshold - segmentLength) / threshold);
        }
    }
}