using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 측면 질주: 줄에 수직으로, 여유가 긴 쪽으로
    [CreateAssetMenu(menuName = "Project F/Pattern Clip/Side Run")]
    public class SideRunClip : FightClip
    {
        [Tooltip("좌우 여유를 비교할 때 살피는 최대 거리 (m)")]
        [Range(0.5f, 16f)] public float lookDistance = 6f;   // FishingSpot.GetFreeDistance 의 검사 상한(16m)
        [Tooltip("로드를 반대쪽으로 틀었다고 볼 최소 각도 (도)")]
        [Range(0f, 90f)] public float counterAngle = 15f;

        public override FishPatternState Type => FishPatternState.SideRun;
    }
}
