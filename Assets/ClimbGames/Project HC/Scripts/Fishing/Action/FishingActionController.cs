using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishingAction
    {
        Idle,
        CastingAim,
        CastingRelease,
    }

    public partial class FishingActionController : FSM<FishingAction>
    {
        public abstract class ActionBase : StateBase
        {
            protected FishingActor Actor { get; private set; }

            protected ActionBase(FishingActor actor, FishingActionController controller) : base(controller)
            {
                Actor = actor;
            }
        }

        public FishingActionController() : base("FishingActor", true)
        {
        }

        public void Initialize(FishingActor actor)
        {
            Initialize(
                (FishingAction.Idle, new IdleAction(actor, this)),
                (FishingAction.CastingAim, new CastingAimAction(actor, this)),
                (FishingAction.CastingRelease, new CastingReleaseAction(actor, this)));
        }
    }
}