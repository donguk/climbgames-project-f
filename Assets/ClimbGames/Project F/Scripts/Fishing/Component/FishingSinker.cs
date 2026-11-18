namespace ClimbGames.ProjectF
{
    // 봉돌: 줄 점 속성(질량, 저항)만 정함 (조인트로 매달리지 않음, FishingActor 가 BindToLine 으로 등록)
    public class FishingSinker : FishingLineObject
    {
        // 컴포넌트를 처음 붙일 때 기본값
        void Reset()
        {
            mass = 0.002f;
            drag = 0.01f;
        }
    }
}
