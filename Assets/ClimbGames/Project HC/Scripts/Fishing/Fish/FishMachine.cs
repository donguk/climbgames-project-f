namespace ClimbGames.Project_HC
{
    public enum FishState
    {
        Idle,
        Approach,           // 물고기
        Nibble,             // 입질 단계
        Bite,               // 바늘을 물었음
        Struggle,
        Submit,
        Flee,
    }

    public partial class FishMachine : FSM<FishState>
    {
        public class FishStateBase : StateBase
        {
            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;

            public FishStateBase(FishingPlayer player, FishMachine fsm) : base(fsm)
            {
                Player = player;
            }
        }

        public FishMachine() : base("<color=blue>Fish</color>", true)
        {
        }

        public virtual void InitializeAndStart(FishingPlayer player)
        {
            Initialize(
                (FishState.Idle, new IdleState(player, this)),
                (FishState.Approach, new ApproachState(player, this)),
                (FishState.Nibble, new NibbleState(player, this)),
                (FishState.Bite, new BiteState(player, this)),
                (FishState.Struggle, new StruggleState(player, this)),
                (FishState.Submit, new SubmitState(player, this)),
                (FishState.Flee, new FleeState(player, this)));

            Start(FishState.Idle);
        }
    }
}