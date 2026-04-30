using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLine : VerletLine
    {
        [SerializeField] Transform lineEnd;

        private FishingRod rod;
        private FishingBait bait;

        public override Transform AnchorTransform => rod != null ? rod.PoleTip : transform;
        public Transform LineEnd => lineEnd;

        public void Setup(FishingRod rod, FishingBait bait)
        {
            this.rod = rod;
            this.bait = bait;
        }

        public override void ManualUpdate(float deltaTime)
        {
            //  

            base.ManualUpdate(deltaTime);

            //
            var endPosition = lineSegments[lineSegments.Count - 1].currentPosition;
            if (lineSegments.Count > 1)
            {
                var beforePosition = lineSegments[lineSegments.Count - 2].currentPosition;
                Vector3 up = beforePosition - endPosition;
                lineEnd.up = up;
            }

            lineEnd.position = endPosition;
        }

        protected override void UpdateAnchor()
        {
            base.UpdateAnchor();

            if (bait != null && bait.State == BaitState.Flying)
            {
                var segment = lineSegments[^1];
                segment.currentPosition = bait.transform.position;
                lineSegments[^1] = segment;
            }
        }
    }
}