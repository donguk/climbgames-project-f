using System;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    [RequireComponent(typeof(LineRenderer))]
    public class FishingLineDrawer : MonoBehaviour, ILineComponent
    {
        // 바로 앞 점과 이 거리보다 가까운 점은 그리지 않음
        private const float MinDrawDistance = 0.005f;

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
            if (positions == null || positions.Length < count)
                positions = new Vector3[count + 10];

            // 일반 LineRenderer: 겹친 점까지 그대로 그림
            bool useSpline = splineDrawer != null && splineDrawer.enabled;
            if (useSpline == false)
            {
                positionCount = count;
                Array.Copy(source, startIndex, positions, 0, count);

                lineRenderer.positionCount = positionCount;
                lineRenderer.SetPositions(positions);
                return;
            }

            // 스플라인: 바로 앞에 넣은 점과 겹친 점은 빼서 복사 (겹친 점에서 스플라인이 꺾이지 않게)
            positionCount = 0;
            float minSqrDistance = MinDrawDistance * MinDrawDistance;
            for (int i = 0; i < count; ++i)
            {
                Vector3 position = source[startIndex + i];
                bool isLast = i == count - 1;

                if (positionCount > 0 && (position - positions[positionCount - 1]).sqrMagnitude < minSqrDistance)
                {
                    // 마지막 점(고정점/끝점)은 위치를 지켜야 하므로 직전 점을 덮어씀 (첫 점은 항상 남음)
                    if (isLast && positionCount > 1)
                        positions[positionCount - 1] = position;
                    continue;
                }

                positions[positionCount++] = position;
            }
        }
    }
}