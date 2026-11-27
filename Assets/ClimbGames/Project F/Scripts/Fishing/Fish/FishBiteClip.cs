using UnityEngine;

namespace ClimbGames
{
    // 입질: 물고기가 바늘을 당기는 힘 (찌의 잠김·기울기·로드 휨은 이 힘이 줄을 타고 전해진 물리 결과)
    [CreateAssetMenu(menuName = "Project HC/Fish BiteClip")]
    public class FishBiteClip : ScriptableObject
    {
        // 바늘을 당기는 힘 (kg, 무게 환산, 찌 buoyancyLoad 와 같은 단위), 가로축은 0 ~ 1 정규화 시간
        // + 는 pullAngle 방향으로 당김, − 는 위로 들어 올림 (올림 입질)
        // 부력 12g·몸통 3cm 찌에 0.004 → 약 1cm 잠김 (잠김 ≈ floatHeight · 당김 / buoyancyLoad)
        public AnimationCurve pull;
        // 당기는 방향: 수평에서 아래로 기운 각도 (90 = 바로 아래라 기울지 않고 잠기기만, 작을수록 옆으로 끌어 찌가 더 기욺)
        [Range(0f, 90f)] public float pullAngle = 70f;
        [Min(0.01f)] public float duration = 2f;   // 재생 시간 (s)
    }
}
