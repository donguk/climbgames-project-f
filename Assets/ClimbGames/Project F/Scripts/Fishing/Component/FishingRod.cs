using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingRod : MonoBehaviour
    {
        [SerializeField] private GameObject model;
        [SerializeField] private Transform poleTip;
        [SerializeField] private FishingRodBender bender;

        [Header("Line Strength")]
        [Tooltip("줄 강도 (N): 실제 팁 장력이 이 값이면 장력 1")]
        [SerializeField, Min(0.1f)] private float breakForce = 10f;

        [Header("Drag")]
        [Tooltip("이 장력을 넘으면 드랙으로 줄이 풀림")]
        [SerializeField, Range(0.1f, 1f)] private float dragThreshold = 0.75f;
        [Tooltip("장력 1 일 때 줄이 풀리는 속도 (m/s)")]
        [SerializeField, Range(0f, 10f)] private float dragSpeed = 1.5f;
        [Tooltip("드랙 중 릴링 효과 배율")]
        [SerializeField, Range(0f, 1f)] private float dragReelScale = 0.3f;

        [Header("Line Break / Slack")]
        [Tooltip("이 장력 이상이거나 드랙 중에 감는 상태가 breakTime 동안 이어지면 줄이 끊김")]
        [SerializeField, Range(0.1f, 1f)] private float breakThreshold = 0.95f;
        [SerializeField, Min(0.1f)] private float breakTime = 1.5f;
        [Tooltip("이 장력 이하가 slackTime 동안 이어지면 바늘이 빠짐")]
        [SerializeField, Range(0f, 0.5f)] private float slackThreshold = 0.02f;
        [SerializeField, Min(0.1f)] private float slackTime = 5f;

        protected GameObject Model => model;
        public Transform PoleTip => poleTip;
        public FishingRodBender Bender => bender;

        public float BreakForce => breakForce;
        public float DragThreshold => dragThreshold;
        public float DragSpeed => dragSpeed;
        public float DragReelScale => dragReelScale;
        public float BreakThreshold => breakThreshold;
        public float BreakTime => breakTime;
        public float SlackThreshold => slackThreshold;
        public float SlackTime => slackTime;

#if UNITY_EDITOR
        // 임계값 순서 유지: slack < drag <= break
        void OnValidate()
        {
            dragThreshold = Mathf.Min(dragThreshold, breakThreshold);
            slackThreshold = Mathf.Clamp(slackThreshold, 0f, Mathf.Max(0f, dragThreshold - 0.05f));
        }
#endif

        // Fighting 게임 텐션: 줄이 당기는 방향(월드)과 텐션(0~1)
        public void SetLoad(Vector3 worldDirection, float tension01)
        {
            if (bender != null)
                bender.SetLoad(worldDirection, tension01);
        }

        public void ClearLoad()
        {
            if (bender != null)
                bender.ClearLoad();
        }
    }
}
