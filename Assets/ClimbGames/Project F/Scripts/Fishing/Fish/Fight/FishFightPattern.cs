using System;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 물고기가 헤엄칠 수 있는 깊이 설정
    [Serializable]
    public struct FishDepthSetting
    {
        [Tooltip("수면에서 최대 깊이 (m)")]
        [Min(0.1f)] public float maxDepth;
        [Tooltip("바닥 위 여유 (m)")]
        [Min(0f)] public float bottomMargin;
        [Tooltip("수면 아래 여유 (m)")]
        [Min(0f)] public float surfaceMargin;
        [Tooltip("이보다 얕은 곳은 헤엄칠 수 없는 곳 (m)")]
        [Min(0f)] public float minSwimDepth;
    }

    // 물고기 한 종의 파이팅 성향 (상태별 값)
    [CreateAssetMenu(menuName = "Project HC/Fish Fight Pattern")]
    public class FishFightPattern : ScriptableObject
    {
        [Serializable]
        public class StateData
        {
            public FishFightState state;
            [Tooltip("지속 시간 범위 (초)")]
            public Vector2 duration = new Vector2(1f, 2f);
            [Tooltip("헤엄 속도 (m/s)")]
            [Min(0f)] public float speed = 1f;
            [Tooltip("버티는 힘: 똑바로 도망칠 때 유지되는 장력 (0~1)")]
            [Range(0.05f, 1f)] public float pullForce = 0.5f;
            [Tooltip("장력 노이즈 배율")]
            [Range(0f, 10f)] public float jitter = 1f;
            [Tooltip("스태미나 소모 배율")]
            [Range(0f, 5f)] public float staminaCost = 1f;
            [Tooltip("목표 깊이 비율 범위 (0 = 수면 바로 아래, 1 = 바닥 / 최대 깊이)")]
            public Vector2 depthRange = new Vector2(0.2f, 0.5f);
            [Tooltip("선택 가중치 (x = 1 - 스태미나)")]
            public AnimationCurve weightByStamina = AnimationCurve.Constant(0f, 1f, 1f);
        }

        [SerializeField] private StateData[] states;

        [Header("Run")]
        [Tooltip("플레이어 반대 방향 기준 질주 방향 후보 범위 (도)")]
        [Range(0f, 90f)] public float runAngle = 60f;
        [Tooltip("질주 방향을 고를 때 앞을 살피는 최대 거리 (m)")]
        [Range(0.5f, 16f)] public float runLookDistance = 6f;   // FishingSpot.GetFreeDistance 의 검사 상한(16m)

        [Header("Stamina")]
        [Tooltip("플레이어가 정답 대응할 때 스태미나 소모 배율")]
        [Min(0f)] public float counteredMultiplier = 2f;
        [Tooltip("플레이어가 대응하지 못할 때 스태미나 소모 배율")]
        [Min(0f)] public float uncounteredMultiplier = 0.5f;
        [Tooltip("휴식 중 감지 않으면 회복되는 스태미나 배율 (소모 속도 기준)")]
        [Min(0f)] public float restRecover = 0.5f;
        [Tooltip("SideRun 에서 로드를 반대쪽으로 틀었다고 볼 최소 각도 (도)")]
        [Range(0f, 90f)] public float sideCounterAngle = 15f;

        [Header("Movement")]
        [Tooltip("최대 상하 속도 (m/s)")]
        [Min(0.01f)] public float verticalSpeed = 1f;
        public FishDepthSetting depth = new FishDepthSetting()
        {
            maxDepth = 3f,
            bottomMargin = 0.2f,
            surfaceMargin = 0.1f,
            minSwimDepth = 0.3f,
        };

        public StateData[] States => states;

#if UNITY_EDITOR
        // 범위 값 보정: x <= y, 지속 시간 최소 0.1초, 깊이 비율 0~1
        void OnValidate()
        {
            if (states == null)
                return;

            foreach (var data in states)
            {
                if (data == null)
                    continue;

                float durationMin = Mathf.Max(0.1f, Mathf.Min(data.duration.x, data.duration.y));
                data.duration = new Vector2(durationMin, Mathf.Max(durationMin, data.duration.y));

                float depthMin = Mathf.Clamp01(Mathf.Min(data.depthRange.x, data.depthRange.y));
                data.depthRange = new Vector2(depthMin, Mathf.Clamp(data.depthRange.y, depthMin, 1f));
            }
        }
#endif

        public StateData Get(FishFightState state)
        {
            if (states != null)
            {
                for (int i = 0; i < states.Length; ++i)
                {
                    if (states[i] != null && states[i].state == state)
                        return states[i];
                }
            }

            return null;
        }
    }
}
