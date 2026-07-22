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

        // 라인이 중력에 의해 쳐지지 않은 정도에서의 길이 계산
        public readonly float TensionedLength => Mathf.Max(0f, straightLength - 1.5f/*slack length*/ - lineLength);
        public readonly float PullSpeed => TensionedLength * 4f;
        public readonly bool IsTensioned => StretchLength > 1f;

        public void Reset()
        {
            straightLength = 0f;
            lineLength = 0f;
        }
    }
}