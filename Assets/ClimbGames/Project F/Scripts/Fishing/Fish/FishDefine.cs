namespace ClimbGames.ProjectF
{
    // 스태미나에 따른 힘 변화 성향
    public enum FishEnduranceType
    {
        Steady,     // 일정: 지친 만큼 고르게 약해짐
        Stubborn,   // 끈질김: 스태미나가 거의 바닥날 때까지 힘을 씀
        Sprinter,   // 초반형: 기운이 남아 있어도 금방 힘이 빠짐
    }

    // 클립 시퀀스를 다 쓴 뒤 규칙 (FishPattern)
    public enum FishPatternEndMode
    {
        Loop,       // 처음부터 다시 (기본)
        Random,     // 이 시퀀스의 클립 중에서 가중치 랜덤
        Once,       // 한 번 돌고 끝 (입질)
    }
}
