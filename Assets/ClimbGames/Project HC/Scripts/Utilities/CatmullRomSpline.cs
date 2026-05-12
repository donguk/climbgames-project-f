using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [Serializable]
    public class CatmullRomSpline
    {
        public bool enabled = true;
        public int interpolation;
        [NonSerialized] public Vector3[] positions;
        public int positionCount;

        public CatmullRomSpline(int interpolation = 5)
        {
            this.interpolation = interpolation;
        }

        public CatmullRomSpline(int interpolation, int capacity) : this(interpolation)
        {
            positions = new Vector3[capacity];
        }

        public void Evaluate(Vector3[] positions, int positionCount)
        {
            this.positionCount = Evaluate(positions, positionCount, ref this.positions, interpolation);
        }

        public static int Evaluate(Vector3[] positions, int positionCount, ref Vector3[] restuls, int interpolation)
        {
            if (positions == null || positions.Length < 2 || positionCount < 2)
                return 0;

            positionCount = Mathf.Min(positions.Length, positionCount);
            int totalPoints = (positionCount - 1) * interpolation + 1;

            if (restuls == null || restuls.Length < totalPoints)
                restuls = new Vector3[totalPoints];

            int index = 0;
            for (int i = 0; i < positionCount - 1; ++i)
            {
                // 끝점을 외삽(extrapolate)해서 자연스럽게
                Vector3 p0 = i > 0 ? positions[i - 1] : positions[0] * 2 - positions[1];
                Vector3 p1 = positions[i];
                Vector3 p2 = positions[i + 1];
                Vector3 p3 = i + 2 < positionCount ? positions[i + 2] : positions[positionCount - 1] * 2 - positions[positionCount - 2];

                for (int j = 0; j < interpolation; j++)
                {
                    float t = j / (float)interpolation;
                    restuls[index++] = Interpolate(t, p0, p1, p2, p3);
                }
            }

            restuls[index++] = positions[positionCount - 1];
            return totalPoints;
        }

        public static Vector3 Interpolate(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // t: 0에서 1 사이의 보간값
            Vector3 a = 2f * p1;
            Vector3 b = p2 - p0;
            Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
            Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

            // 큐빅 베지어 공식과 유사한 3차 다항식 연산
            Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));
            return pos;
        }
    }
}