using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLine : VerletLine
    {
        [SerializeField] Transform lineEnd;
        [SerializeField] private float lineResolution = 0.1f;

        private FishingRod rod;

        public float CurrentLength { get; private set; }
        public override Transform AnchorTransform => rod != null ? rod.PoleTip : transform;
        public Transform LineEnd => lineEnd;

        public void BuildLine(FishingRod rod, float length)
        {
            this.rod = rod;

            BuildLine(length);
        }

        void BuildLine(float length)
        {
            if (lineSegments == null)
                lineSegments = new List<LineSegment>();

            CurrentLength = length;
            segmentCount = Mathf.Max(2, Mathf.RoundToInt(length / lineResolution));

            if (lineSegments.Count < segmentCount)
            {
                int diff = segmentCount - lineSegments.Count;
                for (int i = 0; i < diff; ++i)
                    lineSegments.Add(new LineSegment());
            }
            else
            {
                lineSegments.RemoveRange(segmentCount, lineSegments.Count - segmentCount);
            }
            segmentLength = length / (lineSegments.Count - 1);

            Vector3 position = AnchorTransform.position;
            for (int i = 0; i < lineSegments.Count; ++i)
            {
                var segment = lineSegments[i];
                segment.currentPosition = segment.beforePosition = position;
                lineSegments[i] = segment;

                if (i == lineSegments.Count - 1)
                {
                    lineEnd.up = Vector3.up;
                    lineEnd.position = position;
                }

                position.y -= segmentLength;
            }
        }

        public override void ApplyConstraints()
        {
            base.ApplyConstraints();

            var endPosition = lineSegments[lineSegments.Count - 1].currentPosition;
            if (lineSegments.Count > 1)
            {
                var beforePosition = lineSegments[lineSegments.Count - 2].currentPosition;
                Vector3 up = beforePosition - endPosition;
                lineEnd.up = up;
            }

            lineEnd.position = endPosition;
        }

        public void ApplyBaitConstraint(FishingBait bait)
        {
            if (bait == null)
                return;

            switch (bait.State)
            {
                case BaitState.Flying:
                    {
                        var body = bait.GetComponent<Rigidbody>();
                        if (body.linearVelocity.y > 0f)
                            gravityForce.y = 0f;
                        else
                            gravityForce.y = -0.1f;


                        // 라인 길이 조절
                        float dist = Vector3.Distance(rod.PoleTip.position, bait.transform.position);
                        if (CurrentLength < dist)
                        {
                            float diff = dist - CurrentLength;
                            int count = Mathf.CeilToInt(diff / lineResolution);

                            for (int i = 0; i < count; ++i)
                                lineSegments.Insert(0, new LineSegment(rod.PoleTip.position, rod.PoleTip.position));

                            CurrentLength = dist;
                            segmentLength = dist / (lineSegments.Count - 1);
                            segmentCount = lineSegments.Count;
                        }

                        var segment = lineSegments[^1];
                        segment.beforePosition = segment.currentPosition;
                        segment.currentPosition = bait.transform.position;
                        lineSegments[^1] = segment;

                        break;
                    }
            }
        }
    }
}