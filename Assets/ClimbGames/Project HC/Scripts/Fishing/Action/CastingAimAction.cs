using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActor
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

            public CastingAimAction(FishingActor actor) : base(actor)
            {
            }

            protected override void OnEnter()
            {
                castingInput = ((CastingAimActionParam)param).castingInput;
            }

            protected override void OnUpdate()
            {
                Actor.Character.UpdateCastgAiming(castingInput.delta, castingInput.intensity);
            }
        }
    }
}