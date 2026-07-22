
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class FightingState : FishingActorStateBase
        {
            public FightingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                // 텐션 계산
                float reelingInput = FishingInput.Instance.Reeling;
                FishStatus fishStatus = Fish.Status;

                var tensionInfo = Actor.Line.GetLengthInfo(Fish.Target.MoveTarget);
                float lineTension = tensionInfo.StretchLength / Fish.Data.StretchThreshold;
                if (reelingInput <= 0f)
                    lineTension += Fish.GetTensionNoise();

                fishStatus.lineTension = Mathf.Clamp01(lineTension);
                fishStatus.currentTension = Mathf.MoveTowards(fishStatus.currentTension, fishStatus.lineTension, Time.deltaTime);

                // 텐션 오버 시간
                if (fishStatus.currentTension > Fish.Data.tensionThreshold)
                    fishStatus.tensionOverTime += Time.deltaTime;
                else
                    fishStatus.tensionOverTime = 0f;

                FishingModel.Instance.SetFishStatus(fishStatus);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                // 도망
                if (Fish.CurrentType == FishState.Flee)
                {
                    fsm.ChangeState(FishingActorState.Waiting);
                    return;
                }

                // 잡음
                if (Fish.CurrentType == FishState.Submit)
                {
                    fsm.ChangeState(FishingActorState.Catching);
                    return;
                }

                float reelingInput = FishingInput.Instance.Reeling;
                if (reelingInput > 0f)
                    Actor.Line.Reeling(reelingInput * Time.fixedDeltaTime);

                HookedFish hookedFish = Fish.Target;
                FishData fishData = Fish.Data;
                FishStatus fishStatus = Fish.Status;

                // 물고기가 유지 하려는 텐션
                float fishTension = fishData.tensionCurve.Evaluate(1f - fishStatus.stamina);
                float distance = (fishStatus.lineTension - fishTension) * fishData.StretchThreshold;

                // 릴링 옵션 (강도가 클수록 fishTension 에 빠르게 수렴)
                float intensity = distance * (reelingInput > 0f ? 1f : 4f);
                Vector3 direction = Actor.Rod.PoleTip.position - hookedFish.MoveTarget.position;

                hookedFish.SetTensionInfo(new LineTensionInfo()
                {
                    lineTension = fishStatus.lineTension,
                    fishTension = fishTension,
                    direction = direction,
                    intensity = intensity,
                });
            }
        }
    }
}