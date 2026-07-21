using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct LineLengthInfo
    {
        public static readonly LineLengthInfo Default = new LineLengthInfo();

        // 직선 거리
        public float straightLength;

        // Pinned 라인 길이
        public float lineLength;
        public Vector3 direction;

        // 늘어난 길이
        public readonly float StretchLength => Mathf.Max(0f, straightLength - lineLength);

        public readonly float TensionLength => Mathf.Max(0f, straightLength - 1.2f/*slack distance*/ - lineLength);
        public readonly float PullSpeed => TensionLength * 6f;

        public void Reset()
        {
            straightLength = 0f;
        }
    }
}