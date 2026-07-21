
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class FightState : FishingActorStateBase
        {
            private float currentTension;

            public FightState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                currentTension = 0f;
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
                FishData fishData = Fish.Data;

                // 라인 텐션 계산
                var tensionInfo = Actor.Line.GetLengthInfo(hookedFish.MoveTarget);
                float targetTension = tensionInfo.StretchLength / fishData.StretchThreshold;
                if (reelingInput <= 0f)
                    targetTension += Fish.GetTensionNoise();

                currentTension = Mathf.MoveTowards(currentTension, Mathf.Clamp01(targetTension), Time.fixedDeltaTime);
                FishingModel.Instance.SetTensionValue(currentTension);

                hookedFish.SetTensionInfo(new LineTensionInfo()
                {
                    lineTension = currentTension,
                    targetTension = fishData.tensionCurve.Evaluate(Fish.CurrentStamina),
                    stretchThreshold = fishData.StretchThreshold,
                    direction = Actor.Rod.PoleTip.position - hookedFish.MoveTarget.position,
                    reelPower = 1f,
                });

                // 도망침
                if (currentTension > fishData.tensionThreshold)
                {
                    // 임계점을 넘은 시간이 누적되면 도망침
                    Fish.ChangeState(FishState.Flee);
                    fsm.ChangeState(FishingActorState.Waiting);
                }

                // 잡음
                if (Actor.Line.TotalLength < 1f)
                    fsm.ChangeState(FishingActorState.Catch);
            }
        }
    }
}