using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public enum FishingBaitState
    {
        None,
        Hanging,            // Actor 아이들 상태 중 라인/초릿대에 매달려 있음 (앞서 정한 레이어 분리 상태)
        Flying,             // 캐스팅되어 공중을 날아가는 중 (댐핑 0 상태)
        Floating,           // 물 표면에 안착하여 떠 있는 상태 (부력/대기 상태)
        Nibbling,           // 물고기가 톡톡 건드리는 입질 단계 (Impulse 힘 전달 상태)
        Hooked,             // 물고기가 바늘을 완전히 물었음 (파이팅 대상과 연결된 상태)
    }

    public partial class FishingBaitStateController : FSM<FishingBaitState>
    {
        public class FishingBaitStateBase : StateBase
        {
            protected FishingBait Bait { get; private set; }

            public FishingBaitStateBase(FishingBait bait, FishingBaitStateController controller) : base(controller)
            {
                Bait = bait;
            }
        }

        public class FishingBaitStateBase<T> : StateBase<T> where T : IStateParam
        {
            protected FishingBait Bait { get; private set; }

            public FishingBaitStateBase(FishingBait bait, FishingBaitStateController controller) : base(controller)
            {
                Bait = bait;
            }
        }

        public FishingBaitStateController() : base("BaitState", true)
        {
        }

        public void InitializeAndStart(FishingBait bait)
        {
            Initialize(
                (FishingBaitState.Hanging, new HangingState(bait, this)),
                (FishingBaitState.Flying, new FlyingState(bait, this)),
                (FishingBaitState.Floating, new FloatingState(bait, this)),
                (FishingBaitState.Nibbling, new NibblingState(bait, this)),
                (FishingBaitState.Hooked, new HookedState(bait, this)));

            Start(FishingBaitState.None);
        }
    }
}