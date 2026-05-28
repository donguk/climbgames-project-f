using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLine : VerletLine
    {
        [Header("Fishing Line")]
        [SerializeField] private Transform anchorEnd;
        [SerializeField] private float lengthPerSegment = 1;
        [SerializeField] private float totalLength;
        [SerializeField] private float variableLength;
        [Range(0f, 1f)]
        [SerializeField] private float lineTension = 0f;

        [Header("Spline")]
        [SerializeField] private bool enableSpline;
        [SerializeField] private int splineInterpolation = 5;

        private FishingRod rod;
        private Vector3[] splinePositions;

        protected override Transform AnchorTransform => rod != null ? rod.PoleTip : base.AnchorTransform;
        public float Length => totalLength;
        public Transform End => anchorEnd;

        public void Initialize(FishingRod rod, float length)
        {
            this.rod = rod;

            BuildLine(length);

            anchorEnd.up = Vector3.up;
            anchorEnd.position = GetPosition(positionCount - 1);
        }

        void BuildLine(float length)
        {
            totalLength = Mathf.Max(0.1f, length);

            positionCount = segmentCount + 1;
            segmentLength = totalLength / (positionCount - 1);

            ResizePositionArray(positionCount);

            Vector3 position = AnchorTransform.position;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                position.y -= segmentLength;
            }
        }

        protected override void FixedUpdate()
        {
            // manual update
        }

        protected override void DrawLine()
        {
            if (enableSpline)
            {
                // spline 변환
                int positionCount = CatmullRomSpline.Evaluate(Positions, this.positionCount, ref splinePositions, splineInterpolation);

                // draw line
                lineRenderer.positionCount = positionCount;
                lineRenderer.SetPositions(splinePositions);
            }
            else
            {
                base.DrawLine();
            }
        }

        public override void Constraints()
        {
            base.Constraints();

            if (Positions != null)
            {
                var position = GetPosition(positionCount - 1);

                anchorEnd.position = position;
                anchorEnd.up = (GetPosition(positionCount - 2) - position).normalized;
            }
        }

        protected override void ApplyConstraint()
        {
            for (int i = 0; i < positionCount - 1; ++i)
            {
                Vector3 current = _positions[i];
                Vector3 next = _positions[i + 1];

                float distance = (current - next).magnitude;
                float diff = distance - segmentLength;//(i == 0 ? variableLength : segmentLength);

                Vector3 changeDir = (current - next).normalized;
                Vector3 chagneVector = changeDir * diff;

                if (i != 0)
                {
                    current -= chagneVector * 0.5f;
                    next += chagneVector * 0.5f;
                }
                else
                {
                    next += chagneVector;
                }

                _positions[i] = current;
                _positions[i + 1] = next;
            }
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
        }

        public void PinEnd(Transform target)
        {
            SetPosition(positionCount - 1, target.position);
        }

        public void PinBait(FishingBait bait)
        {
            SetPosition(positionCount - 1, bait.transform.position);
            bait.transform.up = anchorEnd.up;
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
            float distance = Vector3.Distance(rod.PoleTip.position, bait.transform.position);
            float straightSegmentLength = distance / (positionCount - 1);
            float threshold = straightSegmentLength * 0.92f;

            return Mathf.Clamp01((threshold - segmentLength) / threshold);
        }
    }
}