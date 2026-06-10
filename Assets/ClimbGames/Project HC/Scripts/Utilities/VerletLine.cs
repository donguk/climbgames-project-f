using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [RequireComponent(typeof(LineRenderer))]
    public class VerletLine : MonoBehaviour
    {
        public struct Point
        {
            public Vector3 current;
            public Vector3 previous;
        }

        [Header("Line")]
        [SerializeField] protected int positionCount;
        [SerializeField] protected float segmentLength = 0.225f;
        [NonSerialized] protected Vector3[] _positions;
        protected Vector3[] _prevPositions;

        [Header("Physics")]
        [SerializeField] protected Vector3 gravityForce = new Vector3(0f, -2f, 0f);
        [SerializeField] protected float dampingFactor = 0.98f;
        [SerializeField] protected LayerMask collisionMask;
        [SerializeField] protected float collisionRadius = 0.1f;
        [SerializeField] protected float bounceFactor = 0.1f;
        [SerializeField] protected int collisionSegmentInterval = 2;

        [Header("Constraints")]
        [SerializeField] protected int constraintCount = 50;

        protected LineRenderer lineRenderer;
        private Collider[] collisionResults = new Collider[5];

        public Vector3[] Positions => _positions;
        public int PositionCount => positionCount;
        protected virtual Transform AnchorTransform => transform;

        void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ResizePositionArray(positionCount);

            Vector3 position = transform.position;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                position.y -= segmentLength;
            }
        }

        protected virtual void Update()
        {
            DrawLine();
        }

        protected virtual void FixedUpdate()
        {
            Simulate(Time.fixedDeltaTime);
            ApplyConstraint();
        }

        protected void ResizePositionArray(int needSize)
        {
            int padding = 50;

            if (_positions == null)
            {
                _positions = new Vector3[needSize + padding];
            }
            else if (_positions.Length < needSize)
            {
                // 기존 데이터를 유지하면서 크기를 확장 (내부적으로 복사 발생)
                Array.Resize(ref _positions, needSize + padding);
            }

            if (_prevPositions == null)
            {
                _prevPositions = new Vector3[needSize + padding];
            }
            else if (_prevPositions.Length < needSize)
            {
                Array.Resize(ref _prevPositions, needSize + padding);
            }
        }

        public void SetPosition(int index, Vector3 position)
        {
            if (_positions != null && index >= 0 && index < _positions.Length)
                _positions[index] = position;
        }

        public Vector3 GetPosition(int index)
        {
            if (_positions != null && index >= 0 && index < _positions.Length)
                return _positions[index];

            return Vector3.zero;
        }

        public void InsertPosition(int index, Vector3 position, Vector3 previous)
        {
            ResizePositionArray(positionCount + 1);

            Array.Copy(_positions, index, _positions, index + 1, positionCount - index);
            Array.Copy(_prevPositions, index, _prevPositions, index + 1, positionCount - index);

            _positions[index] = position;
            _prevPositions[index] = previous;

            positionCount++;
        }

        public void InsertPositions(int index, List<Point> points)
        {
            if (points == null || points.Count <= 0)
                return;

            ResizePositionArray(positionCount + points.Count);

            Array.Copy(_positions, index, _positions, index + points.Count, positionCount - index);
            Array.Copy(_prevPositions, index, _prevPositions, index + points.Count, positionCount - index);

            for (int i = 0; i < points.Count; ++i)
            {
                _positions[index + i] = points[i].current;
                _prevPositions[index + i] = points[i].previous;
            }

            positionCount += points.Count;
        }

        public void RemovePosition(int startIndex, int count = 1)
        {
            count = Mathf.Min(count, positionCount - startIndex);

            if (startIndex + count < positionCount)
            {
                Array.Copy(_positions, startIndex + count, _positions, startIndex, positionCount - (startIndex + count));
                Array.Copy(_prevPositions, startIndex + count, _prevPositions, startIndex, positionCount - (startIndex + count));
            }

            positionCount -= count;
        }

        protected virtual void DrawLine()
        {
            if (lineRenderer == null)
                return;

            lineRenderer.positionCount = positionCount;
            lineRenderer.SetPositions(_positions);
        }

        public void Simulate(float deltaTime)
        {
            for (int i = 0; i < positionCount; ++i)
            {
                Vector3 velocity = (_positions[i] - _prevPositions[i]) * dampingFactor;
                _prevPositions[i] = _positions[i];

                _positions[i] += velocity;
                _positions[i] += gravityForce * deltaTime * deltaTime;
            }
        }

        public virtual void ApplyConstraint()
        {
            SetPosition(0, AnchorTransform.position);

            for (int i = 0; i < constraintCount; ++i)
            {
                ApplySegmentLength();

                if (i % collisionSegmentInterval == 0)
                    HandleCollisions();
            }
        }

        protected virtual void ApplySegmentLength()
        {
            for (int i = 0; i < positionCount - 1; ++i)
            {
                Vector3 current = _positions[i];
                Vector3 next = _positions[i + 1];

                float distance = (current - next).magnitude;
                float diff = distance - segmentLength;

                Vector3 changeDir = (current - next).normalized;
                Vector3 chagneVector = changeDir * diff;

                if (i != 0)
                {
                    current -= chagneVector * 0.5f;
                    next += chagneVector * 0.5f;
                }
                else
                {
                    next += chagneVector;
                }

                _positions[i] = current;
                _positions[i + 1] = next;
            }
        }

        void HandleCollisions()
        {
            for (int i = 0; i < positionCount; ++i)
            {
                Vector3 position = _positions[i];
                Vector3 velocity = position - _prevPositions[i];
                int count = Physics.OverlapSphereNonAlloc(position, collisionRadius, collisionResults, collisionMask);

                for (int j = 0; j < count; ++j)
                {
                    Collider col = collisionResults[j];

                    Vector3 closestPoint = col.ClosestPoint(position);
                    float distance = Vector3.Distance(position, closestPoint);

                    if (distance < collisionRadius)
                    {
                        Vector3 normal = (position - closestPoint).normalized;
                        if (normal == Vector3.zero)
                        {
                            normal = (position - col.transform.position).normalized;
                        }

                        float depth = collisionRadius - distance;
                        position += normal * depth;

                        velocity = Vector3.Reflect(velocity, normal) * bounceFactor;
                    }
                }

                _positions[i] = position;
                _prevPositions[i] = position - velocity;
            }
        }
    }
}