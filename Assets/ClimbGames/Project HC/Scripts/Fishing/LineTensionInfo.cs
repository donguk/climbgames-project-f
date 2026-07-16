using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct LineTensionInfo
    {
        public static readonly LineTensionInfo Default = new LineTensionInfo();

        /// <summary>
        /// PoleTip -> Target 직선 거리
        /// </summary>
        public float straightLength;

        /// <summary>
        /// PoleTip -> Pinned 라인 길이
        /// </summary>
        public readonly float lineLength;

        /// <summary>
        /// 직선거리보다 이만큼 짧아져야 텐션 시작
        /// </summary>
        public readonly float slackDistance;

        /// <summary>
        /// PullDistance가 이 값이 되면 텐션 = 1
        /// </summary>
        public readonly float maxPullDistance;

        /// <summary>
        /// 텐션 방향
        /// </summary>
        public readonly Vector3 lineDirection;

        /// <summary>
        /// 텐션이 시작되는 라인 길이
        /// </summary>
        public float TensionStartLength => straightLength - slackDistance;

        public float IdleSlackLength => straightLength - slackDistance * 0.8f;

        /// <summary>
        /// 현재 얼마나 더 끌어와야 하는지 (m)
        /// </summary>
        public float PullDistance => Mathf.Max(0f, TensionStartLength - lineLength);

        /// <summary>
        /// 0~1
        /// </summary>
        public float TensionValue => maxPullDistance > 0f ? Mathf.Clamp01(PullDistance / maxPullDistance) : 0f;

        public LineTensionInfo(float straightLength, float lineLength, Vector3 lineDirection, float slackDistance, float maxPullDistance)
        {
            this.straightLength = straightLength;
            this.lineLength = lineLength;
            this.lineDirection = lineDirection;
            this.slackDistance = slackDistance;
            this.maxPullDistance = maxPullDistance;
        }

        public void Reset()
        {
            straightLength = 0f;
        }
    }
}