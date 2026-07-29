
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorMachine
    {
        public class FightingState : ActorStateBase
        {
            public FightingState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 도망
                Player.MessageBroker.Receive<FishingEvent.OnFishFlee>().Subscribe(_ => fsm.ChangeState(FishingActorState.Waiting)).AddTo(this);

                // 물고기 잡음
                Player.MessageBroker.Receive<FishingEvent.OnFishSubmit>().Subscribe(_ => fsm.ChangeState(FishingActorState.Catching)).AddTo(this);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                // 텐션 계산
                float reelingValue = Player.Model.reelingValue;
                FishingPlayerModel model = Player.Model;
                FishData fishData = Player.FishData;

                var tensionInfo = Actor.Line.GetLengthInfo(Fish.MoveTarget);
                float lineTension = tensionInfo.StretchLength / fishData.StretchThreshold;
                if (reelingValue <= 0f)
                    lineTension += Player.GetFishNoise();

                model.lineTension = Mathf.Clamp01(lineTension);
                model.currentTension = Mathf.MoveTowards(model.currentTension, model.lineTension, Time.deltaTime);

                // 텐션 오버 시간
                if (model.currentTension > fishData.tensionThreshold)
                    model.tensionOverTime += Time.deltaTime;
                else
                    model.tensionOverTime = 0f;
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingValue = Player.Model.reelingValue;
                if (reelingValue > 0f)
                    Actor.Line.Reeling(reelingValue * Time.fixedDeltaTime);

                FishingPlayerModel model = Player.Model;
                HookedFish hookedFish = Player.Fish;
                FishData fishData = Player.FishData;

                // 물고기가 유지 하려는 텐션
                float fishTension = fishData.tensionCurve.Evaluate(1f - model.FishStatus.stamina);
                float distance = (model.lineTension - fishTension) * fishData.StretchThreshold;

                // 릴링 옵션 (강도가 클수록 fishTension 에 빠르게 수렴)
                float intensity = distance * (reelingValue > 0f ? 1f : 4f);
                Vector3 direction = Actor.Rod.PoleTip.position - hookedFish.MoveTarget.position;

                hookedFish.SetTensionInfo(new LineTensionInfo()
                {
                    lineTension = model.lineTension,
                    fishTension = fishTension,
                    direction = direction,
                    intensity = intensity,
                });
            }
        }
    }
}