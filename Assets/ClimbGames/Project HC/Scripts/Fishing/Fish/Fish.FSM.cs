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

    public partial class Fish : FSM<FishState>
    {
        public class FishStateBase : StateBase
        {
            protected Fish Fish { get; private set; }
            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;

            public FishStateBase(Fish fish, FishingPlayer player) : base(fish)
            {
                Fish = fish;
                Player = player;
            }
        }

        public virtual void InitializeAndStart(FishingPlayer player)
        {
            Initialize(
                (FishState.Idle, new IdleState(this, player)),
                (FishState.Approach, new ApproachState(this, player)),
                (FishState.Nibble, new NibbleState(this, player)),
                (FishState.Bite, new BiteState(this, player)),
                (FishState.Struggle, new StruggleState(this, player)),
                (FishState.Submit, new SubmitState(this, player)),
                (FishState.Flee, new FleeState(this, player)));

            Start(FishState.Idle);
        }
    }
}