
using R3.Triggers;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActor
    {
        public struct CastingReleaseActionParam : IStateParam
        {
            public CastingInput castingInput;

            public CastingReleaseActionParam(CastingInput castingInput)
            {
                this.castingInput = castingInput;
            }
        }

        public class CastingReleaseAction : ActionBase
        {
            private float targetAngle;
            private bool isReturning;
            private Quaternion pullRotation;
            private float swingElapsed, swingTime;

            public CastingReleaseAction(FishingActor actor) : base(actor)
            {
            }

            protected override void OnEnter()
            {
                var intensity = ((CastingReleaseActionParam)param).castingInput.intensity;
                targetAngle = 20f/*minimum angle*/ + 10f * intensity;
                pullRotation = Actor.Character.Arm.localRotation;
                swingElapsed = 0f;
                swingTime = 0.2f - (0.05f * intensity);
                isReturning = false;
            }

            protected override void OnUpdate()
            {
                if (isReturning == false)
                {
                    swingElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(swingElapsed / swingTime);
                    float easedT = t * (2f - t);
                    var targetRotation = Quaternion.Euler(targetAngle, 0, 0);
                    Actor.Character.Arm.localRotation = Quaternion.Slerp(pullRotation, targetRotation, easedT);

                    if (t >= 1f)
                    {
                        isReturning = true;
                        swingElapsed = 0f;
                    }
                }
                else
                {
                    //
                }
            }
        }
    }
}