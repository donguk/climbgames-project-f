using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public partial class FishingLine : VerletLine
    {
        [Header("Fishing Line")]
        [SerializeField] private Transform hookAnchor;
        [SerializeField] private Transform bobberAnchor;
        [SerializeField] private float totalLength;
        [Range(0f, 1f)]
        [SerializeField] private float lineTension = 0f;
        [SerializeField] private float bobberHeight;

        [Header("Spline")]
        [SerializeField] private bool enableSpline;
        [SerializeField] private int splineInterpolation = 5;

        private Transform rodTipAnchor;
        private Vector3[] splinePositions;
        private Transform pinnedTarget;

        protected override Transform AnchorTransform => rodTipAnchor != null ? rodTipAnchor : base.AnchorTransform;
        public float Length => totalLength;
        public float VisualLength => pinnedTarget != null ? Vector3.Distance(AnchorTransform.position, pinnedTarget.position) : totalLength;
        public Transform End => hookAnchor;
        public Transform Bobber => bobberAnchor;

        void Start()
        {
            enableSpline = false;
        }

        public void BuildLine(Transform rodTip, float length)
        {
            rodTipAnchor = rodTip;
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

        void UpdateAnchors()
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
                    UpdateAnchor(bobberAnchor, i + 1, segmentLength - diff);
                    break;
                }
            }

            // end
            UpdateAnchor(hookAnchor, positionCount - 1);
        }

        void UpdateAnchor(Transform transform, int positionIndex, float distance = default)
        {
            if (positionCount > 1 && transform != null)
            {
                Vector3 position = GetPosition(positionIndex);

                transform.up = (GetPosition(positionIndex - 1) - position).normalized;
                transform.position = position + transform.up * distance;
            }
        }

        protected override void FixedUpdate()
        {
            // update by actor
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

        public override void ApplyConstraint()
        {
            base.ApplyConstraint();

            ApplyTension();
            UpdateAnchors();
        }

        protected override void ApplySegmentLength()
        {
            for (int i = 0; i < positionCount - 1; ++i)
            {
                int index = i;

                Vector3 current = _positions[index];
                Vector3 next = _positions[index + 1];

                float distance = (current - next).magnitude;
                float diff = distance - segmentLength;

                Vector3 changeDir = (current - next).normalized;
                Vector3 chagneVector = changeDir * diff;

                if (index != 0)
                {
                    current -= chagneVector * 0.5f;
                    next += chagneVector * 0.5f;
                }
                else
                {
                    next += chagneVector;
                }

                _positions[index] = current;
                _positions[index + 1] = next;
            }

            if (pinnedTarget != null)
                SetPosition(positionCount - 1, pinnedTarget.position);
        }

        void ApplyTension()
        {
            if (lineTension > 0f)
            {
                Vector3 startPos = _positions[0];
                Vector3 endPos = _positions[positionCount - 1];

                for (int i = 1; i < positionCount - 1; ++i)
                {
                    // 1. 현재 점이 전체 길이에서 차지하는 비율 (0.0 ~ 1.0)
                    float t = (float)i / (positionCount - 1);

                    // 2. 완벽하게 팽팽한 직선일 때의 위치
                    Vector3 straightLinePos = Vector3.Lerp(startPos, endPos, t);

                    // 3. tension 수치에 따라 자연스러운 처짐과 직선 사이를 섞어줌
                    _positions[i] = Vector3.Lerp(_positions[i], straightLinePos, lineTension);
                }
            }
        }

        public void SetEnvironment(Vector3 force, float damping)
        {
            gravityForce = force;
            dampingFactor = damping;
        }

        public void PinEnd(Transform target)
        {
            pinnedTarget = target;
            UpdateSegment();
        }

        public void PinBait(FishingBait bait)
        {
            SetPosition(positionCount - 1, bait.transform.position);
            bait.transform.up = hookAnchor.up;
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
            float distance = Vector3.Distance(rodTipAnchor.position, bait.transform.position);
            float straightSegmentLength = distance / (positionCount - 1);
            float threshold = straightSegmentLength * 0.92f;

            return Mathf.Clamp01((threshold - segmentLength) / threshold);
        }
    }
}