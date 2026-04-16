namespace ClimbGames.Project_HC
{
    public enum FishingRodAction
    {
        Idle,
        Load,
        Cast,
    }

    public partial class FishingRodActor : FSM<FishingRodAction>
    {
        public abstract class ActionBase : StateBase
        {
            protected FishingRodActor Actor { get; private set; }

            protected ActionBase(FishingRodActor actor) : base(actor)
            {
                Actor = actor;
            }
        }

        public FishingRod Target { get; private set; }

        public FishingRodActor() : base("RodActor", true)
        {
        }

        public void Initialize(FishingRod target)
        {
            Target = target;

            Initialize(
                (FishingRodAction.Idle, new IdleAction(this)),
                (FishingRodAction.Load, new LoadAction(this)),
                (FishingRodAction.Cast, new CastAction(this)));
        }
    }
}