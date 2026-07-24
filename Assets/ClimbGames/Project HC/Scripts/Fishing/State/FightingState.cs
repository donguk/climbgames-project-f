
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingPlayer
    {
        public class FightingState : FishingPlayerStateBase
        {
            public FightingState(FishingPlayer player, Fish fish, FishingPlayer fsm) : base(player, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 도망
                Player.MessageBroker.Receive<FishingEvent.OnFishFlee>().Subscribe(_ => fsm.ChangeState(FishingPlayerState.Waiting)).AddTo(this);

                // 물고기 잡음
                Player.MessageBroker.Receive<FishingEvent.OnFishSubmit>().Subscribe(_ => fsm.ChangeState(FishingPlayerState.Catching)).AddTo(this);
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
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

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