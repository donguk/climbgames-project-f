using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActionController
    {
        public struct CastingAimActionParam : IStateParam
        {
            public CastingInput castingInput;

            public CastingAimActionParam(CastingInput castingInput)
            {
                this.castingInput = castingInput;
            }
        }

        public class CastingAimAction : ActionBase
        {
            private CastingInput castingInput;

            public CastingAimAction(FishingActor actor, FishingActionController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                castingInput = ((CastingAimActionParam)param).castingInput;
                //Actor.Character.FadeLayerWeight(1f);
            }

            protected override void OnUpdate()
            {
                Actor.CameraController.UpdateRotation(castingInput.delta);
            }

            protected override void OnFixedUpdate()
            {
                //Actor.Character.UpdateCastgAiming(castingInput.delta);
            }
        }
    }
}