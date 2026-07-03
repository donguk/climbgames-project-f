using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public enum FishingLineState
    {
        None,
        Hanging,            // 아이들 상태
        Flying,             // 날아가는 중
        Settled,            // 수면 바닥에 닿음

        Floating,           // 물 표면에 안착하여 떠 있는 상태 (부력/대기 상태)
        Nibbling,           // 물고기가 톡톡 건드리는 입질 단계 (Impulse 힘 전달 상태)
        Hooked,             // 물고기가 바늘을 완전히 물었음 (파이팅 대상과 연결된 상태)
    }

    public partial class FishingLineController : FSM<FishingLineState>
    {
        public class FishingLineStateBase : StateBase
        {
            protected FishingLine Manager { get; private set; }

            public FishingLineStateBase(FishingLine manager, FishingLineController controller) : base(controller)
            {
                Manager = manager;
            }
        }

        public class FishingLineStateBase<T> : StateBase<T> where T : IStateParam
        {
            protected FishingLine Manager { get; private set; }

            public FishingLineStateBase(FishingLine manager, FishingLineController controller) : base(controller)
            {
                Manager = manager;
            }
        }

        public FishingLineController() : base("Line", true)
        {
        }

        public void InitializeAndStart(FishingLine manager, FishingLineState state)
        {
            Initialize(
                (FishingLineState.Hanging, new HangingState(manager, this)),
                (FishingLineState.Flying, new FlyingState(manager, this)),
                (FishingLineState.Settled, new SettledState(manager, this)),

                (FishingLineState.Floating, new FloatingState(manager, this)),
                (FishingLineState.Nibbling, new NibblingState(manager, this)),
                (FishingLineState.Hooked, new HookedState(manager, this)));

            Start(state);
        }
    }
}