namespace ClimbGames.ProjectF
{
    // 봉돌: 줄 점 속성(질량, 저항)만 정함 (AttachToLine 을 override 해 조인트 없이 등록)
    public class FishingSinker : FishingLineObject
    {
        // 조인트로 매달지 않고 줄 점에 장비로만 등록
        public override void AttachToLine(FishingLinePoint linePoint)
        {
            LinePoint = linePoint;
            if (linePoint.Segment != null)
                linePoint.Segment.controller = this;
        }

        // 컴포넌트를 처음 붙일 때 기본값: 2g 납 봉돌 (지름 약 7mm, 무거운 봉돌은 지름도 같이 키움: 10g 약 12mm)
        void Reset()
        {
            mass = 0.002f;
            bodyDiameter = 0.007f;
            bodyDensity = 11340f;   // 납
        }
    }
}
