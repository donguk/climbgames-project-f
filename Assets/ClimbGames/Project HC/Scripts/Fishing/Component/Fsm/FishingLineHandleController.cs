using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public enum FishingLineHandleState
    {
        None,
        Hanging,            // Actor 아이들 상태 중 라인/초릿대에 매달려 있음 (앞서 정한 레이어 분리 상태)
        Flying,             // 캐스팅되어 공중을 날아가는 중 (댐핑 0 상태)
        Floating,           // 물 표면에 안착하여 떠 있는 상태 (부력/대기 상태)
        Nibbling,           // 물고기가 톡톡 건드리는 입질 단계 (Impulse 힘 전달 상태)
        Hooked,             // 물고기가 바늘을 완전히 물었음 (파이팅 대상과 연결된 상태)
    }

    public partial class FishingLineHandleController : FSM<FishingLineHandleState>
    {
        public class FishingLineHandleStateBase : StateBase
        {
            protected FishingLineHandle Handle { get; private set; }

            public FishingLineHandleStateBase(FishingLineHandle handle, FishingLineHandleController controller) : base(controller)
            {
                Handle = handle;
            }
        }

        public class FishingLineHandleStateBase<T> : StateBase<T> where T : IStateParam
        {
            protected FishingLineHandle Handle { get; private set; }

            public FishingLineHandleStateBase(FishingLineHandle handle, FishingLineHandleController controller) : base(controller)
            {
                Handle = handle;
            }
        }

        public FishingLineHandleController() : base("LineHandle", true)
        {
        }

        public void InitializeAndStart(FishingLineHandle handle, FishingLineHandleState state)
        {
            Initialize(
                (FishingLineHandleState.Hanging, new HangingState(handle, this)),
                (FishingLineHandleState.Flying, new FlyingState(handle, this)),
                (FishingLineHandleState.Floating, new FloatingState(handle, this)),
                (FishingLineHandleState.Nibbling, new NibblingState(handle, this)),
                (FishingLineHandleState.Hooked, new HookedState(handle, this)));

            Start(state);
        }
    }
}