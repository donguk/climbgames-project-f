using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine : VerletLine
    {
        [Header("Spline")]
        [SerializeField] private bool enableSpline;
        [SerializeField] private int splineInterpolation = 5;

        private Vector3[] splinePositions;

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
            UpdatePoints();
        }

        //protected override void ApplySegmentLength()
        //{
        //    base.ApplySegmentLength();
        //
        //    if (lineHandle.HasForce)
        //        SetPosition(positionCount - 1, lineHandle.Target.transform.position);
        //}

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
    }
}