using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum VerletUpdateMode
    {
        Update,
        FixedUpdate,
        ManualUpdate,
    }

    [RequireComponent(typeof(LineRenderer))]
    public class VerletLine : MonoBehaviour
    {
        public struct LineSegment
        {
            public Vector3 currentPosition;
            public Vector3 beforePosition;

            public LineSegment(Vector3 currentPosition, Vector3 beforePosition)
            {
                this.currentPosition = currentPosition;
                this.beforePosition = beforePosition;
            }
        }

        [Header("Line")]
        [SerializeField] protected int segmentCount = 50;
        [SerializeField] protected float segmentLength = 0.225f;

        [Header("Physics")]
        [SerializeField] private VerletUpdateMode updateMode = VerletUpdateMode.FixedUpdate;
        [SerializeField] private Vector3 gravityForce = new Vector3(0f, -2f, 0f);
        [SerializeField][Range(0f, 1f)] private float dampingFactor = 0.98f;
        [SerializeField] private LayerMask collisionMask;
        [SerializeField] private float collisionRadius = 0.1f;
        [SerializeField] private float bounceFactor = 0.1f;
        [SerializeField] private int collisionSegmentInterval = 2;

        [Header("Constraints")]
        [SerializeField] private int constraintCount = 50;

        protected LineRenderer lineRenderer;
        protected List<LineSegment> lineSegments;

        public List<LineSegment> LineSegments => lineSegments;
        public virtual Transform AnchorTransform => transform;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = segmentCount;

            BuildSegment();
        }

        void Update()
        {
            if (updateMode == VerletUpdateMode.Update)
                SimulateAndConstraints(Mathf.Min(Time.deltaTime, Time.fixedDeltaTime));

            DrawLine();
        }

        public virtual void FixedUpdate()
        {
            if (updateMode == VerletUpdateMode.FixedUpdate)
                SimulateAndConstraints(Time.fixedDeltaTime);
        }

        public void BuildSegment()
        {
            if (lineSegments == null)
                lineSegments = new List<LineSegment>();
            lineSegments.Clear();

            Vector3 position = transform.position;
            for (int i = 0; i < segmentCount; ++i)
            {
                lineSegments.Add(new LineSegment(position, position));
                position.y -= segmentLength;
            }
        }

        void DrawLine()
        {
            for (int i = 0; i < lineSegments.Count; ++i)
                lineRenderer.SetPosition(i, lineSegments[i].currentPosition);
        }

        void SimulateAndConstraints(float deltaTime)
        {
            Simulate(deltaTime);

            UpdateAnchor();

            for (int i = 0; i < constraintCount; ++i)
            {
                ApplyConstraint();

                if (i % collisionSegmentInterval == 0)
                    HandleCollisions();
            }
        }

        protected virtual void UpdateAnchor()
        {
            Transform anchorTransform = AnchorTransform;
            if (lineSegments != null && lineSegments.Count > 0)
            {
                var segment = lineSegments[0];
                segment.currentPosition = anchorTransform.position;
                lineSegments[0] = segment;
            }
        }

        public virtual void ManualUpdate(float deltaTime) => SimulateAndConstraints(deltaTime);

        void Simulate(float deltaTime)
        {
            for (int i = 0; i < lineSegments.Count; ++i)
            {
                LineSegment segment = lineSegments[i];
                Vector3 velocity = (segment.currentPosition - segment.beforePosition) * dampingFactor;
                segment.beforePosition = segment.currentPosition;

                segment.currentPosition += velocity;
                segment.currentPosition += gravityForce * deltaTime;
                lineSegments[i] = segment;
            }
        }

        void ApplyConstraint()
        {
            for (int i = 0; i < lineSegments.Count - 1; ++i)
            {
                LineSegment currentSegment = lineSegments[i];
                LineSegment nextSegment = lineSegments[i + 1];

                float distance = (currentSegment.currentPosition - nextSegment.currentPosition).magnitude;
                float diff = (distance - segmentLength);

                Vector3 changeDir = (currentSegment.currentPosition - nextSegment.currentPosition).normalized;
                Vector3 chagneVector = changeDir * diff;

                if (i != 0)
                {
                    currentSegment.currentPosition -= chagneVector * 0.5f;
                    nextSegment.currentPosition += chagneVector * 0.5f;
                }
                else
                {
                    nextSegment.currentPosition += chagneVector;
                }

                lineSegments[i] = currentSegment;
                lineSegments[i + 1] = nextSegment;
            }
        }

        void HandleCollisions()
        {
            for (int i = 0; i < lineSegments.Count; ++i)
            {
                LineSegment segment = lineSegments[i];
                Vector3 velocity = segment.currentPosition - segment.beforePosition;
                Collider[] colliders = Physics.OverlapSphere(segment.currentPosition, collisionRadius, collisionMask);

                foreach (var col in colliders)
                {
                    Vector3 closestPoint = col.ClosestPoint(segment.currentPosition);
                    float distance = Vector3.Distance(segment.currentPosition, closestPoint);

                    if (distance < collisionRadius)
                    {
                        Vector3 normal = (segment.currentPosition - closestPoint).normalized;
                        if (normal == Vector3.zero)
                        {
                            normal = (segment.currentPosition - col.transform.position).normalized;
                        }

                        float depth = collisionRadius - distance;
                        segment.currentPosition += normal * depth;

                        velocity = Vector3.Reflect(velocity, normal) * bounceFactor;
                    }
                }

                segment.beforePosition = segment.currentPosition - velocity;
                lineSegments[i] = segment;
            }
        }
    }
}