using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 줄 물리(FishingLineBody)가 로드 팁을 스프링에 묶인 질점으로 풀 때 로드 쪽이 주는 값
    public interface IRodTip
    {
        // 팁 앵커 (월드): 줄 물리가 팁 질점을 이 위치에 스프링으로 묶음
        // - 그립(root)의 지금 위치·회전 + 정적 휨(로드 자중, Fighting 게임 텐션)만 반영한 목표 팁 위치
        // - 지금 휜 각도(줄 장력·관성·떨림으로 휜 것)는 쓰지 않음: 쓰면 휨 → 앵커 → 휨 되먹임이 생김
        // - 로드를 움직이면 앵커는 바로 따라가고, 질량이 있는 팁이 뒤처진 만큼 휨이 됨
        Vector3 GetTipAnchor();

        // 팁에서 줄 쪽(팁 → 다음 줄 점) 월드 방향 (줄 물리가 Step 전에 지난 스텝 결과로 넘김, zero 면 없음)
        // 로드 끝 접선은 줄이 당기는 방향을 넘어서 휘지 않으므로 휨 한계에 씀
        void SetLineDirection(Vector3 direction);

        // 팁 스프링 속성 (진동수, 감쇠, 기준 하중·처짐, 최대 처짐)
        FishingLineBody.TipSpring GetTipSpring();

        // 줄이 푼 팁 위치에 맞춰 본을 휘고, 실제 메시 팁 위치를 돌려줌
        Vector3 ApplyTip(Vector3 simulatedTip);
    }
}
