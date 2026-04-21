using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishingAction
    {
        Idle,
        CastingAim,
        CastRelease,
    }

    public partial class FishingActor : FSM<FishingAction>
    {
        public abstract class ActionBase : StateBase
        {
            protected FishingActor Actor { get; private set; }

            protected ActionBase(FishingActor actor) : base(actor)
            {
                Actor = actor;
            }
        }

        private Quaternion baseRotation;
        private Vector2 currentView;

        public FishingCharacter Character { get; private set; }
        public FishingRod Rod { get; private set; }

        public FishingActor() : base("FishingActor", true)
        {
        }

        public void Initialize(FishingCharacter character, FishingRod rod)
        {
            Character = character;
            Rod = rod;

            Initialize(
                (FishingAction.Idle, new IdleAction(this)),
                (FishingAction.CastingAim, new CastingAimAction(this)),
                (FishingAction.CastRelease, new CastingReleaseAction(this)));
        }
    }
}