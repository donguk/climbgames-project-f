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

        private SplineDrawer splineDrawer;
        private Transform rodTip;
        private LinearBlender segmentBlender = new LinearBlender(0.4f);

        protected override Transform AnchorTransform => rodTip != null ? rodTip : base.AnchorTransform;
        public float Length => totalLength;
        public float VisualLength => lineHandle.HasTarget ? Vector3.Distance(AnchorTransform.position, lineHandle.transform.position) : totalLength;

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
            segmentBlender.Reset(true);

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

        protected override void DrawLine()
        {
            if (splineDrawer == null || splineDrawer.enabled == false)
                base.DrawLine();
        }

        protected override void FixedUpdate()
        {
            // simulate is called by actor
            //
            segmentBlender.Update(Time.fixedDeltaTime);
        }

        public override void ApplyConstraint()
        {
            base.ApplyConstraint();

            // 텐션 적용
            ApplyTension();

            // handle poistion 업데이트
            lineHandle.UpdatePosition();

            // 세그먼트 계산
            UpdateSegment();
            bobberPoint.UpdatePosition();
        }

        void ApplyTension()
        {
            if (lineTension > 0f)
            {
                Vector3 startPos = _positions[0];
                Vector3 endPos = _positions[positionCount - 1];

                for (int i = 1; i < positionCount - 1; ++i)
                {
                    float t = (float)i / (positionCount - 1);
                    Vector3 straightLinePos = Vector3.Lerp(startPos, endPos, t);
                    _positions[i] = Vector3.Lerp(_positions[i], straightLinePos, lineTension);
                }
            }
        }

        public void UpdateSegment()
        {
            float lineLength = VisualLength;
            float targetLength = lineLength / (positionCount - 1);
            segmentLength = Mathf.Lerp(segmentLength, targetLength, segmentBlender.Value);
        }

        public void SetHandleTarget(Transform target)
        {
            lineHandle.SetTarget(target);
            segmentBlender.Reset(target != null);
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
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

        public float CalculateTension(FishingLineHandle bait)
        {
            float distance = Vector3.Distance(rodTip.position, bait.transform.position);
            float straightSegmentLength = distance / (positionCount - 1);
            float threshold = straightSegmentLength * 0.92f;

            return Mathf.Clamp01((threshold - segmentLength) / threshold);
        }
    }
}