using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public struct CastingAimActionParam : IStateParam
        {
            public CastingInput castingInput;

            public CastingAimActionParam(CastingInput castingInput)
            {
                this.castingInput = castingInput;
            }
        }

        public class CastingAimAction : FishingActorStateBase
        {
            private CastingInput castingInput;

            public CastingAimAction(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                castingInput = ((CastingAimActionParam)param).castingInput;
            }

            protected override void OnUpdate()
            {
                Actor.CameraController.UpdateRotation(castingInput.delta);
            }
        }
    }
}