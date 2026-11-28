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

    // 물고기 한 종의 기본 데이터 (엑셀 한 행 모양: 열은 평탄하게, 기본 타입·enum·패턴 목록만)
    // 표 데이터이므로 읽기 전용으로 씀
    [Serializable]
    public class FishSpec
    {
        public int id;
        public string name;

        // 힘·지구력
        [Min(1f)] public float staminaDuration = 20f;     // 기본 소모로 다 닳는 시간 (초)
        public FishEnduranceType endurance = FishEnduranceType.Steady;
        [Range(0f, 1f)] public float minPowerScale = 0.2f;      // 완전히 지쳤을 때 속도 배율 (0~1)
        [Min(0f)] public float speedScale = 1f;           // 같은 패턴 asset 에 곱하는 물고기별 배율
        [Min(0f)] public float pullScale = 1f;

        // 스태미나 소모 규칙
        [Min(0f)] public float counteredMultiplier = 1.5f;    // 플레이어가 정답 대응할 때 소모 배율
        [Min(0f)] public float uncounteredMultiplier = 0.3f;  // 대응하지 못할 때 소모 배율
        [Min(0f)] public float restRecover = 0.5f;            // 휴식 중 감지 않으면 회복 배율 (소모 속도 기준)

        // 장력 노이즈
        [Range(0f, 0.2f)] public float tensionJitterIntensity = 0.05f;
        [Min(0f)] public float tensionJitterFrequency = 10f;

        // 이동·깊이
        [Min(0.01f)] public float verticalSpeed = 1f;        // 최대 상하 속도 (m/s)
        [Min(0.1f)] public float maxDepth = 3f;
        [Min(0f)] public float bottomMargin = 0.2f;
        [Min(0f)] public float surfaceMargin = 0.1f;
        [Min(0f)] public float minSwimDepth = 0.3f;

        // 행동 시퀀스: 클립 키(asset 이름) 목록, 순서대로 실행 (엑셀에서는 한 칸 "Run,SideRun,Rest" 처럼)
        public string[] biteClips = { "Bite" };                     // 입질 시퀀스 (Once 로 한 번 재생)
        [Range(0f, 1f)] public float biteChance = 1f;             // 입질이 끝났을 때 바늘을 완전히 물 확률 (나머지는 놓고 떠남)
        public string[] fightClips;                                 // 파이팅 시퀀스 (중복 가능)
        public FishPatternEndMode fightEndMode = FishPatternEndMode.Loop;       // Loop 또는 Random (Once 는 파이팅에 쓰지 않음)

        public FishDepthSetting Depth => new FishDepthSetting()
        {
            maxDepth = Mathf.Max(0.1f, maxDepth),   // 표 값은 인스펙터 범위를 거치지 않으므로 여기서도 막음
            bottomMargin = Mathf.Max(0f, bottomMargin),
            surfaceMargin = Mathf.Max(0f, surfaceMargin),
            minSwimDepth = Mathf.Max(0f, minSwimDepth),
        };
    }
}
