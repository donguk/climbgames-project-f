using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 머리 흔들기: 제자리에서 좌우로
    [CreateAssetMenu(menuName = "Project F/Pattern Clip/Head Shake")]
    public class HeadShakeClip : FightClip
    {
        public override FishPatternState Type => FishPatternState.HeadShake;
    }
}
