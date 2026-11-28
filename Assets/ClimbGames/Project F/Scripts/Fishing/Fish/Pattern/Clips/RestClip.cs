using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 휴식: 힘을 빼고 끌려옴
    [CreateAssetMenu(menuName = "Project F/Pattern Clip/Rest")]
    public class RestClip : FightClip
    {
        [Tooltip("이 장력(0~1)을 넘게 당기면 휴식을 끝내고 다시 도망 (Run 우선)")]
        [Range(0f, 1f)] public float breakOutTension = 0.9f;

        public override FishPatternState Type => FishPatternState.Rest;
    }
}
