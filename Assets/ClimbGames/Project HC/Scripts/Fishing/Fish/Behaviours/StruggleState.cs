using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishMachine
    {
        public class StruggleState : FishStateBase
        {
            private HookedFish targetFish;
            private float pullAngleThreshold = Mathf.Cos(30f * Mathf.Deg2Rad);

            public StruggleState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 생성
                targetFish = Player.CreateHookedFish(Actor.Hook);

                Player.MessageBroker.Publish(new FishingEvent.OnFishBite() { hookedFish = targetFish });
            }

            protected override void OnFixedUpdate()
            {
                float reelingValue = Player.Model.reelingValue;
                FishingPlayerModel model = Player.Model;
                FishData fishData = Player.FishData;

                // 물고기 스테미나 감소
                float decreaseStamina = fishData.staminaDuration > 0f ? 1f / fishData.staminaDuration : 0f;

                Vector3 characterToFish = targetFish.MoveTarget.position - Actor.transform.position;
                characterToFish.y = 0f;
                float pullAngle = Vector3.Dot(characterToFish.normalized, Actor.Rod.transform.forward);
                if (reelingValue > 0f || pullAngle < pullAngleThreshold)
                    decreaseStamina *= 2f;

                model.FishStatus.DecreaseStamina(decreaseStamina * Time.fixedDeltaTime);

                // 스테미나에 따른 이동범위 제한
                float stamina = fishData.staminaCurve.Evaluate(1f - model.FishStatus.stamina);
                float struggleRange = Mathf.Max(0.4f/*최소범위*/, fishData.moveRange * stamina);

                var areaInfo = Player.WaterArea.GetInnerAreaInfo(targetFish.MoveTarget.position, struggleRange);
                targetFish.SetStruggleInfo(new FishStruggleInfo()
                {
                    areaInfo = areaInfo,
                    speed = Mathf.Max(0.2f/*최소속도*/, fishData.moveSpeed * stamina),
                    stamina = stamina,
                });

                //  
                if (model.tensionOverTime > 1f)
                    fsm.ChangeState(FishState.Flee);

                // 
                if (Actor.Line.TotalLength < 1f)
                    fsm.ChangeState(FishState.Submit);
            }
        }
    }
}