using UnityEngine;

namespace ClimbGames.Project_HC
{
    interface ILineComponent
    {
        Vector3[] Positions { get; }
        int PositionCount { get; }
    }


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

        void OnDisable()
        {
            lineRenderer.positionCount = 0;
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