using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 잠수: 바닥 쪽으로 (깊이는 depthRange)
    [CreateAssetMenu(menuName = "Project F/Pattern Clip/Dive")]
    public class DiveClip : FightClip
    {
        public override FishPatternState Type => FishPatternState.Dive;
    }
}
