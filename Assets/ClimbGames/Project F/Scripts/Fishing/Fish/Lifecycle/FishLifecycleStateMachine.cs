namespace ClimbGames.ProjectF
{
    public enum FishLifecycleState
    {
        Idle,
        Approach,           // 물고기
        Bite,               // 입질: 끝나면 확률로 바늘을 완전히 물거나(Fight) 놓고 떠남(Idle)
        Fight,              // 바늘을 문 물고기의 파이팅
        Submit,
        Flee,
    }

    // 물고기의 생애 주기. 각 단계는 OnEnter 에서 FishPattern 의 시퀀스를 다시 설정해서 행동함
    public partial class FishLifecycleStateMachine : FSM<FishLifecycleState>
    {
        private readonly FishPattern pattern = new FishPattern();

        public FishPattern Pattern => pattern;

        public class LifecycleStageBase : StateBase
        {
            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;
            protected FishPattern Pattern => ((FishLifecycleStateMachine)fsm).Pattern;

            public LifecycleStageBase(FishingPlayer player, FishLifecycleStateMachine fsm) : base(fsm)
            {
                Player = player;
            }
        }

        public FishLifecycleStateMachine() : base("<color=blue>Fish</color>", false)
        {
        }

        public virtual void InitializeAndStart(FishingPlayer player)
        {
            Initialize(
                (FishLifecycleState.Idle, new IdleState(player, this)),
                (FishLifecycleState.Approach, new ApproachState(player, this)),
                (FishLifecycleState.Bite, new BiteState(player, this)),
                (FishLifecycleState.Fight, new FightState(player, this)),
                (FishLifecycleState.Submit, new SubmitState(player, this)),
                (FishLifecycleState.Flee, new FleeState(player, this)));

            Start(FishLifecycleState.Idle);
        }
    }
}
