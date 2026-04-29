
using System;
using com.pickplay.carryons;
using R3;
using R3.Triggers;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActionController
    {
        public struct CastingReleaseActionParam : IStateParam
        {
            public CastingInput castingInput;
            public event Action onThrowBait;

            public CastingReleaseActionParam(CastingInput castingInput)
            {
                this.castingInput = castingInput;
                onThrowBait = null;
            }

            public void OnThrowBait() => onThrowBait?.Invoke();
        }

        public class CastingReleaseAction : ActionBase
        {
            private int throwHash;
            private bool isThrowBait, isApplyWeight;

            public CastingReleaseAction(FishingActor actor, FishingActionController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                throwHash = Animator.StringToHash("Throw");
                isThrowBait = isApplyWeight = false;
                //Actor.Character.PlayAnimation("Throw");
            }

            protected override void OnUpdate()
            {
                //var stateInfo = Actor.Character.CurrentAnimatorStateInfo;
                //if (stateInfo.shortNameHash == throwHash)
                //{
                //    if (stateInfo.normalizedTime > 0.5f && isThrowBait == false)
                //    {
                //        ((CastingReleaseActionParam)param).OnThrowBait();
                //        isThrowBait = true;
                //    }
                //
                //    if (stateInfo.normalizedTime > 0.7f && isApplyWeight == false)
                //    {
                //        Actor.Character.FadeLayerWeight(0.5f);
                //        isApplyWeight = true;
                //    }
                //}
            }
        }
    }
}