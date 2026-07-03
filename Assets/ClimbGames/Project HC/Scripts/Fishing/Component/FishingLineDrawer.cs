using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLineDrawer : MonoBehaviour, ILineComponent
    {
        private LineRenderer lineRenderer;
        private SplineDrawer splineDrawer;
        private Vector3[] positions;
        private int positionCount;

        public Vector3[] Positions => positions;
        public int PositionCount => positionCount;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            splineDrawer = GetComponent<SplineDrawer>();
            positions = new Vector3[50];
        }

        public void DrawLine(Vector3[] source, int startIndex, int count)
        {
            positionCount = count;

            if (positions == null || positions.Length < count)
                positions = new Vector3[count + 10];

            Array.Copy(source, startIndex, positions, 0, count);

            if (splineDrawer == null || splineDrawer.enabled == false)
            {
                lineRenderer.positionCount = positionCount;
                lineRenderer.SetPositions(positions);
            }
        }
    }
}