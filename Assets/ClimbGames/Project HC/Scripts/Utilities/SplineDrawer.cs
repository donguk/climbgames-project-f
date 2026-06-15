using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public class SplineDrawer : MonoBehaviour
    {
        [SerializeField] private int interpolation = 5;

        private ILineComponent lineComponent;
        private LineRenderer lineRenderer;
        private Vector3[] buf;

        void Awake()
        {
            lineComponent = GetComponent<ILineComponent>();
            lineRenderer = GetComponent<LineRenderer>();
        }

        void Update()
        {
            if (lineComponent != null)
            {
                // spline 변환
                int positionCount = CatmullRomSpline.Evaluate(lineComponent.Positions, lineComponent.PositionCount, ref buf, interpolation);

                // draw line
                lineRenderer.positionCount = positionCount;
                lineRenderer.SetPositions(buf);
            }
        }
    }
}