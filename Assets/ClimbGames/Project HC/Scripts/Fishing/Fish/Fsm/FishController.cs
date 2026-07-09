namespace ClimbGames.Project_HC
{
    public enum FishState
    {
        None,
        Idle,
        Interest,         // 물고기 관심
        Nibble,           // 물고기가 톡톡 건드리는 입질 단계
        Bite,             // 물고기가 바늘을 완전히 물었음
    }

    public partial class FishController : FSM<FishState>
    {
        public class FishStateBase : StateBase
        {
            protected FishingActor Actor { get; private set; }

            public FishStateBase(FishingActor actor, FishController fsm) : base(fsm)
            {
                Actor = actor;
            }
        }

        public FishController() : base("Fish", true)
        {
        }

        public void InitializeAndStart(FishingActor actor)
        {
            Initialize(
                (FishState.Idle, new IdleState(actor, this)),
                (FishState.Interest, new InterestState(actor, this)),
                (FishState.Nibble, new NibbleState(actor, this)),
                (FishState.Bite, new BiteState(actor, this)));

            Start(FishState.Idle);
        }
    }
}