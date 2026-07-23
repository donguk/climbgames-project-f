using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public class VerletLineRenderer : MonoBehaviour
    {
        [SerializeField] private VerletLine verletLine;
        protected LineRenderer lineRenderer;

        protected void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            verletLine.BuildLine(transform.position);
        }

        protected virtual void Update()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.positionCount = verletLine.PositionCount;
            lineRenderer.SetPositions(verletLine.Positions);
        }

        protected virtual void FixedUpdate()
        {
            verletLine.Simulate(Time.fixedDeltaTime);

            verletLine.SetPosition(0, transform.position);
            verletLine.ApplyConstraint();
        }
    }
}