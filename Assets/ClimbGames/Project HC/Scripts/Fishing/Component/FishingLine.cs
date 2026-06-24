using System.ComponentModel;
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
        [ReadOnly(true)]
        [SerializeField] private float lineTension = 0f;
        [Range(0f, 1f)]
        [SerializeField] private float forcedTension = 0f;

        private SplineDrawer splineDrawer;
        private Transform rodTip;
        private LinearTween segmentTween = new LinearTween(0.4f);

        protected override Transform AnchorTransform => rodTip != null ? rodTip : base.AnchorTransform;
        public float Length => totalLength;
        public float Tension => lineTension;
        public FishingLineHandle Handle => lineHandle;

        public FishingLinePoint BobberPoint => bobberPoint;
        public FishingLinePoint HookPoint => lineHandle;

        protected override void Awake()
        {
            base.Awake();

            splineDrawer = GetComponent<SplineDrawer>();
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

            segmentTween.Reset(true);
            UpdateSegment();

            Vector3 position = AnchorTransform.position;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                position.y -= segmentLength;
            }
        }

        protected override void DrawLine()
        {
            if (splineDrawer == null || splineDrawer.enabled == false)
                base.DrawLine();
        }

        protected override void FixedUpdate()
        {
            // simulate is called by actor
            //
            segmentTween.Update(Time.fixedDeltaTime);
        }

        public void UpdateSegment()
        {
            float straightLength = Vector3.Distance(AnchorTransform.position, lineHandle.transform.position);
            float defaultSegment = straightLength / (positionCount - 1);

            float visualLength = lineHandle.HasTarget ? straightLength : totalLength;
            float targetSegment = visualLength / (positionCount - 1);

            lineTension = 0f;
            if (lineHandle.HasForce)
            {
                float tensionLength = defaultSegment * 0.9f;
                if (targetSegment > tensionLength)
                    targetSegment = Mathf.Lerp(targetSegment, tensionLength, forcedTension);

                lineTension = Mathf.Clamp01((defaultSegment - targetSegment) / (defaultSegment - tensionLength));
            }

            segmentLength = Mathf.Lerp(segmentLength, targetSegment, segmentTween.Value);
        }

        public void SetLength(float length)
        {
            totalLength = length;
            segmentTween.Reset();
        }

        public void SetHandleTarget(Transform target)
        {
            lineHandle.SetTarget(target);
            segmentTween.Reset(target != null);
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
        }

        public void Release(float amount)
        {
            if (amount > 0f)
                totalLength += amount;
        }

        public void Reeling(float amount)
        {
            if (amount > 0f)
                totalLength = Mathf.Max(0.5f, totalLength - amount);
        }
    }
}