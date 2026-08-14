using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace ClimbGames.ProjectF
{
    [Serializable]
    public class FishingLineBody : VerletLine
    {
        public class LinePoint
        {
            private static ObjectPool<LinePoint> pool = new ObjectPool<LinePoint>(() => new LinePoint(), (x) => x.Reset());
            public static LinePoint Get() => pool.Get();
            public static void Release(LinePoint instance) => pool.Release(instance);

            public int currentIndex;
            public float segmentLength;
            public bool isDynamic;
            public Transform pinTarget;

            public bool IsPinned => currentIndex == 0 || pinTarget != null;

            public void Reset()
            {
                currentIndex = -1;
                segmentLength = 0f;
                isDynamic = false;
                pinTarget = null;
            }
        }

        private List<LinePoint> linePoints = new List<LinePoint>();
        private float forcedTension = 0f;

        public IReadOnlyList<LinePoint> Points => linePoints;
        public float VisualLength => linePoints.Sum(x => x.segmentLength);
        public float ForcedTension => forcedTension;

        public void BuildLine(Vector3 anchorPosition, float length)
        {
            float remainLength = length % segmentLength;
            int segmentCount = (int)(length / segmentLength) + (remainLength > 0f ? 1 : 0);

            // 클리어
            linePoints.ForEach(x => LinePoint.Release(x));
            linePoints.Clear();
            ResizePositionArray(positionCount = segmentCount + 1);

            Vector3 position = anchorPosition;
            for (int i = 0; i < positionCount; ++i)
            {
                _positions[i] = position;
                _prevPositions[i] = position;

                // 메타 데이터 생성
                var linePoint = LinePoint.Get();
                linePoint.currentIndex = i;
                if (i < segmentCount)
                    linePoint.segmentLength = i == 0 && remainLength > 0f ? remainLength : segmentLength;
                linePoints.Add(linePoint);

                position.y -= linePoints[i].segmentLength;
            }
        }

        public void SetForcedTension(float tension)
        {
            forcedTension = tension;
        }

        public void ApplyConstraint(Vector3 anchorPosition)
        {
            SetPosition(0, anchorPosition);
            ApplyConstraint();
        }

        protected override void ApplySegmentLength()
        {
            for (int i = 0; i < positionCount - 1; ++i)
            {
                Vector3 current = _positions[i];
                Vector3 next = _positions[i + 1];

                LinePoint currentPoint = linePoints[i];
                LinePoint nextPoint = linePoints[i + 1];

                float distance = (current - next).magnitude;
                float segmentLength = currentPoint.segmentLength * (1f - forcedTension);
                float diff = distance - segmentLength;

                Vector3 changeDir = (current - next).normalized;
                Vector3 chagneVector = changeDir * diff;

                if (currentPoint.IsPinned && nextPoint.IsPinned)
                {
                    continue;
                }
                else if (currentPoint.IsPinned)
                {
                    next += chagneVector;
                }
                else if (nextPoint.IsPinned)
                {
                    current -= chagneVector;
                }
                else
                {
                    current -= chagneVector * 0.5f;
                    next += chagneVector * 0.5f;
                }

                _positions[i] = current;
                _positions[i + 1] = next;
            }
        }

        public void SetLength(float length)
        {
            float visualLength = VisualLength;
            if (visualLength < length)
            {
                AddLength(length - visualLength);
            }
            else
            {
                RemoveLength(visualLength - length);
            }
        }

        public float GetLength(int index)
        {
            float length = 0f;
            int count = Mathf.Min(index, linePoints.Count);
            for (int i = 0; i < count; ++i)
                length += linePoints[i].segmentLength;
            return length;
        }

        public bool TryGetPinnedLength(out float length)
        {
            length = 0f;
            for (int i = 0; i < linePoints.Count; ++i)
            {
                length += linePoints[i].segmentLength;
                if (i > 0 && linePoints[i].IsPinned)
                    return true;
            }
            return false;
        }

        void AddLength(float amount)
        {
            if (linePoints.Count > 0)
            {
                var linePoint = linePoints[0];
                float needLength = segmentLength - linePoint.segmentLength;
                linePoint.segmentLength = Mathf.Min(linePoint.segmentLength + amount, segmentLength);
                amount -= needLength;
            }

            if (amount > 0f)
            {
                int addCount = (int)(amount / segmentLength) + 1;
                for (int i = 0; i < addCount; ++i)
                {
                    Vector3 position = GetPosition(0);
                    InsertPosition(0, position, position);

                    var linePoint = LinePoint.Get();
                    linePoint.segmentLength = segmentLength < amount ? segmentLength : amount;
                    linePoints.Insert(0, linePoint);

                    amount -= linePoint.segmentLength;
                }

                for (int i = 0; i < linePoints.Count; ++i)
                    linePoints[i].currentIndex = i;
            }
        }

        void RemoveLength(float amount)
        {
            bool isRemoved = false;
            while (amount > 0f && linePoints.Count > 0)
            {
                var linePoint = linePoints[0];
                if (linePoint.segmentLength <= amount)
                {
                    RemovePosition(0);

                    LinePoint.Release(linePoint);
                    linePoints.RemoveAt(0);

                    amount -= linePoint.segmentLength;
                    isRemoved = true;
                }
                else
                {
                    linePoint.segmentLength -= amount;
                    break;
                }
            }

            if (isRemoved)
            {
                for (int i = 0; i < linePoints.Count; ++i)
                    linePoints[i].currentIndex = i;
            }
        }

        public LinePoint AddPinPoint(FishingLinePoint pinTarget)
        {
            LinePoint pinPoint = null;

            float accumulatedHeight = 0f, pinEpsilon = segmentLength * 0.3f;
            for (int i = linePoints.Count - 2; i >= 0; --i)
            {
                var currentPoint = linePoints[i];
                if (pinTarget.Height < accumulatedHeight + currentPoint.segmentLength || i == 0)
                {
                    Vector3 position = GetPosition(i + 1);
                    Vector3 up = (GetPosition(i) - position).normalized;

                    float dist = Mathf.Clamp(pinTarget.Height - accumulatedHeight, 0f, currentPoint.segmentLength);
                    if (dist < pinEpsilon)
                    {
                        // 1. i + 1 번 점(아래쪽 점)과 거의 일치하는 경우
                        pinPoint = linePoints[i + 1];
                        pinPoint.pinTarget = pinTarget.transform;
                        break;
                    }

                    if (currentPoint.segmentLength - dist < pinEpsilon)
                    {
                        // 2. i 번 점(위쪽 점)과 거의 일치하는 경우
                        pinPoint = currentPoint;
                        pinPoint.pinTarget = pinTarget.transform;
                        break;
                    }

                    position += up * dist;
                    InsertPosition(i + 1, position, position);
                    currentPoint.segmentLength -= dist;

                    pinPoint = LinePoint.Get();
                    pinPoint.segmentLength = dist;
                    pinPoint.pinTarget = pinTarget.transform;
                    pinPoint.isDynamic = true;
                    linePoints.Insert(i + 1, pinPoint);

                    for (int j = i + 1; j < linePoints.Count; ++j)
                        linePoints[j].currentIndex = j;

                    break;
                }

                accumulatedHeight += currentPoint.segmentLength;
            }

            return pinPoint;
        }

        public void RemovePinPoint(LinePoint pinPoint)
        {
            if (pinPoint != null)
                RemovePinPoint(pinPoint.currentIndex);
        }

        public void RemovePinPoint(int pinIndex)
        {
            if (pinIndex >= 0 && pinIndex < linePoints.Count)
            {
                var pinPoint = linePoints[pinIndex];

                pinPoint.pinTarget = null;
                if (pinPoint.isDynamic)
                {
                    // 시작, 끝 점 제외
                    if (pinIndex > 0 && pinIndex < linePoints.Count - 1)
                        linePoints[pinIndex - 1].segmentLength += pinPoint.segmentLength;

                    linePoints.RemoveAt(pinIndex);
                    RemovePosition(pinIndex);

                    for (int i = pinIndex; i < linePoints.Count; ++i)
                        linePoints[i].currentIndex = i;
                }
            }
        }
    }
}