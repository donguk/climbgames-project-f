using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 질주: 플레이어 반대쪽 중 수역 여유가 가장 긴 방향으로
    [CreateAssetMenu(menuName = "Project F/Pattern Clip/Run")]
    public class RunClip : FightClip
    {
        [Tooltip("플레이어 반대 방향 기준 질주 방향 후보 범위 (도)")]
        [Range(0f, 90f)] public float runAngle = 60f;
        [Tooltip("질주 방향을 고를 때 앞을 살피는 최대 거리 (m)")]
        [Range(0.5f, 16f)] public float lookDistance = 6f;   // FishingSpot.GetFreeDistance 의 검사 상한(16m)

        public override FishPatternState Type => FishPatternState.Run;
    }
}
