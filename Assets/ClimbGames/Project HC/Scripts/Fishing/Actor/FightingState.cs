
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class FightingState : FishingActorStateBase
        {
            private float currentTension;
            private float pullAngleThreshold;

            public FightingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                currentTension = 0f;
                pullAngleThreshold = Mathf.Cos(30f * Mathf.Deg2Rad);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingInput = FishingInput.Instance.Reeling;
                if (reelingInput > 0f)
                {
                    // 릴 감기
                    Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime);
                }

                HookedFish hookedFish = Fish.Target;
                var fishStatus = Fish.Status;
                var fishData = Fish.Data;
                float stretchThreshold = Fish.Data.StretchThreshold;
                float fishStamina = fishData.staminaCurve.Evaluate(1f - fishStatus.stamina);

                // 텐션 게이지
                var tensionInfo = Actor.Line.GetTensionInfo(hookedFish.StruggleTarget);
                float targetTension = Mathf.Max(tensionInfo.StretchLength / stretchThreshold, 0.05f);
                targetTension = Mathf.Clamp01(targetTension);
                if (reelingInput <= 0f)
                {
                    // 노이즈 생성
                    float currentJitter = Fish.Data.tensionJitterIntensity * fishStamina;
                    float noise = (Mathf.PerlinNoise(Time.time * Fish.Data.tensionJitterFrequency, 0f) * 2f) - 1f;
                    float noiseTension = noise * currentJitter;
                    targetTension = Mathf.Clamp01(targetTension + noiseTension);
                }
                currentTension = Mathf.MoveTowards(currentTension, targetTension, 1f/*speed*/ * Time.fixedDeltaTime);
                FishingModel.Instance.SetTensionValue(currentTension);



                // 물고기의 목표 텐션을 유지를 위한 움직임
                float maintainTension = fishData.tensionCurve.Evaluate(fishStamina);
                float tensionDiff = currentTension - maintainTension;
                float restoreDist = tensionDiff * stretchThreshold;

                Vector3 moveDir = Actor.Rod.PoleTip.position - hookedFish.StruggleTarget.position;
                if (moveDir.sqrMagnitude > 0.0001f)
                {
                    float acceleration = 1f; //
                    Vector3 moveStep = moveDir.normalized * restoreDist;
                    hookedFish.StruggleTarget.position += moveStep * acceleration * Time.deltaTime;
                }



                // 물고기 스테미나 감소
                Vector3 rodForward = Actor.Rod.transform.forward;
                Vector3 characterToFish = hookedFish.StruggleTarget.position - Actor.transform.position;
                characterToFish.y = 0f;

                float pullAngle = Vector3.Dot(characterToFish.normalized, rodForward);
                if (reelingInput > 0f || pullAngle < pullAngleThreshold)
                {
                    fishStatus.stamina = Mathf.Clamp01(fishStatus.stamina - fishData.staminaDrainRate * Time.deltaTime);
                }


                // 도망침
                if (currentTension > Fish.Data.tensionThreshold)
                {

                }


                // 잡음
                {

                }
            }
        }
    }
}