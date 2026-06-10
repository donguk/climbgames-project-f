using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine : VerletLine
    {
        [Header("Fishing")]
        [SerializeField] private LineSocket hookSocket;
        [SerializeField] private LineSocket bobberSocket;
        [SerializeField] private float totalLength;
        [Range(0f, 1f)]
        [SerializeField] private float lineTension = 0f;
        [SerializeField] private float bobberHeight;

        private Transform rodTip;
        private Transform pinTarget;

        protected override Transform AnchorTransform => rodTip != null ? rodTip : base.AnchorTransform;
        public float Length => totalLength;
        public float VisualLength => pinTarget != null ? Vector3.Distance(AnchorTransform.position, pinTarget.position) : totalLength;
        public Transform End => hookSocket.transform;
        public LineSocket Bobber => bobberSocket;
        public LineSocket Hook => hookSocket;

        void Start()
        {
            enableSpline = false;
        }

        public void BuildLine(Transform rodTip, float length)
        {
            this.rodTip = rodTip;
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

        public void SetBobberHeight(float height)
        {
            bobberHeight = height;
        }

        public void UpdateSegment()
        {
            segmentLength = VisualLength / (positionCount - 1);
        }

        void UpdateSockets()
        {
            float visualLength = VisualLength;
            segmentLength = visualLength / (positionCount - 1);

            // bobber
            float bobberLength = visualLength - (visualLength * (bobberHeight / totalLength));
            for (int i = 0; i < positionCount - 1; ++i)
            {
                float diff = bobberLength - (segmentLength * i);
                if ((diff >= 0f && diff <= segmentLength) || (i == 0 && diff < 0f) || (i == positionCount - 2))
                {
                    diff = Mathf.Clamp(diff, 0f, segmentLength);
                    UpdateSocket(bobberSocket, i + 1, segmentLength - diff);
                    break;
                }
            }

            // end
            UpdateSocket(hookSocket, positionCount - 1);
        }

        void UpdateSocket(LineSocket socket, int positionIndex, float distance = default)
        {
            if (positionIndex >= 0 && positionIndex < positionCount && positionCount > 1)
            {
                Vector3 position = GetPosition(positionIndex);

                socket.transform.up = (GetPosition(positionIndex - 1) - position).normalized;
                socket.transform.position = position + transform.up * distance;
            }
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
        }

        public void SetPinTarget(Transform target)
        {
            pinTarget = target;
            UpdateSegment();
        }

        public void PinBait(FishingBait bait)
        {
            SetPosition(positionCount - 1, bait.transform.position);
            bait.transform.up = hookSocket.transform.up;
        }

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

        public float CalculateTension(FishingBait bait)
        {
            float distance = Vector3.Distance(rodTip.position, bait.transform.position);
            float straightSegmentLength = distance / (positionCount - 1);
            float threshold = straightSegmentLength * 0.92f;

            return Mathf.Clamp01((threshold - segmentLength) / threshold);
        }
    }
}