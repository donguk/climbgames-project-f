using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 파이팅 상태가 HookedFish 에 전달하는 물고기의 의도
    public struct FishIntentInfo
    {
        public FishFightState state;

        /// <summary>
        /// 헤엄 방향 (수평, 수역 조향이 끝난 값)
        /// </summary>
        public Vector3 heading;

        /// <summary>
        /// 헤엄 속도 (m/s)
        /// </summary>
        public float swimSpeed;

        /// <summary>
        /// 목표 깊이 비율 (0 = 수면 바로 아래, 1 = 바닥 / 최대 깊이)
        /// </summary>
        public float depthRatio;

        /// <summary>
        /// 버티는 힘 (0~1): 똑바로 도망칠 때 유지되는 장력
        /// </summary>
        public float pullForce;

        /// <summary>
        /// 줄에 끌려오는 정도: 줄 힘 1N 당 끌려오는 속도 (m/s per N)
        /// </summary>
        public float restoreGain;

        /// <summary>
        /// 장력 노이즈 배율
        /// </summary>
        public float jitter;
    }
}
