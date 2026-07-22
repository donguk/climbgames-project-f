using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct LineLengthInfo
    {
        public static readonly LineLengthInfo Default = new LineLengthInfo();

        /// <summary>
        /// 직선 거리
        /// </summary>
        public float straightLength;

        /// <summary>
        /// 라인 길이
        /// </summary>
        public float lineLength;
        public Vector3 direction;

        public float pullLength;
        public float pullSpeed;

        /// <summary>
        /// 늘어난 길이
        /// </summary>
        public readonly float StretchLength => Mathf.Max(0f, straightLength - lineLength);
        public readonly bool IsTensioned => StretchLength > 1f;

        public void Reset()
        {
            straightLength = 0f;
            lineLength = 0f;
            pullLength = 0f;
            pullSpeed = 0f;
        }
    }
}