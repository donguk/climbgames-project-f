using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLine : VerletLine
    {
        [SerializeField] Transform lineEnd;

        private FishingRod rod;
        private FishingBait bait;

        public override Transform AnchorTransform => rod != null ? rod.PoleTip : transform;

        public void Setup(FishingRod rod, FishingBait bait)
        {
            this.rod = rod;
            this.bait = bait;
        }

        //protected override void DrawLine()
        //{
        //    float alpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
        //    Vector3 delta = startPoint.position - fixedStartPosition;
        //
        //    //if (delta.magnitude > 0f)
        //    //{
        //    //    for (int i = 0; i < lineSegments.Count; ++i)
        //    //    {
        //    //        var segment = lineSegments[i];
        //    //        segment.currentPosition += delta * alpha;
        //    //        segment.beforePosition += delta * alpha;
        //    //    }
        //    //
        //    //    fixedStartPosition += delta * alpha;
        //    //}
        //    //
        //    //base.DrawLine();
        //
        //    for (int i = 0; i < lineSegments.Count; ++i)
        //    {
        //        lineRenderer.SetPosition(i, lineSegments[i].currentPosition + delta);
        //    }
        //
        //    var endPosition = lineSegments[lineSegments.Count - 1].currentPosition + delta;
        //    if (lineSegments.Count > 1)
        //    {
        //        var beforePosition = lineSegments[lineSegments.Count - 2].currentPosition + delta;
        //        Vector3 up = beforePosition - endPosition;
        //        lineEnd.up = up;
        //    }
        //
        //    lineEnd.position = endPosition;
        //}

        //protected override void LateUpdate()
        //{
        //    base.LateUpdate();
        //
        //    if (lineSegments.Count > 0)
        //    {
        //        Vector3 delta = startPoint.position - fixedStartPosition;
        //
        //
        //    }
        //}


    }
}