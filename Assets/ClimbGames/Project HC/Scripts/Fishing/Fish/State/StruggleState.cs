using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class Fish
    {
        public class StruggleState : FishStateBase
        {
            private HookedFish targetFish;
            private float pullAngleThreshold = Mathf.Cos(30f * Mathf.Deg2Rad);

            public StruggleState(Fish fish, FishingPlayer player) : base(fish, player)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 생성
                targetFish = Fish.CreateHookedFish(Actor.Hook);

                Player.MessageBroker.Publish(new FishingEvent.OnFishBite() { hookedFish = targetFish });
            }

            protected override void OnFixedUpdate()
            {
                float reelingInput = FishingInput.Instance.Reeling;
                FishStatus fishStatus = Fish.Status;

                // 물고기 스테미나 감소
                float decreaseStamina = Fish.Data.staminaDuration > 0f ? 1f / Fish.Data.staminaDuration : 0f;

                Vector3 characterToFish = targetFish.MoveTarget.position - Actor.transform.position;
                characterToFish.y = 0f;
                float pullAngle = Vector3.Dot(characterToFish.normalized, Actor.Rod.transform.forward);
                if (reelingInput > 0f || pullAngle < pullAngleThreshold)
                    decreaseStamina *= 2f;

                fishStatus.DecreaseStamina(decreaseStamina * Time.fixedDeltaTime);

                // 스테미나에 따른 이동범위 제한
                float currentStamina = Fish.CurrentStamina;
                float struggleRange = Mathf.Max(0.4f/*최소범위*/, Fish.Data.moveRange * currentStamina);

                var areaInfo = Actor.WaterArea.GetInnerAreaInfo(targetFish.MoveTarget.position, struggleRange);
                targetFish.SetStruggleInfo(new FishStruggleInfo()
                {
                    areaInfo = areaInfo,
                    speed = Mathf.Max(0.2f/*최소속도*/, Fish.Data.moveSpeed * currentStamina),
                    stamina = currentStamina,
                });

                //  
                if (fishStatus.tensionOverTime > 1f)
                    fsm.ChangeState(FishState.Flee);

                // 
                if (Actor.Line.TotalLength < 1f)
                    fsm.ChangeState(FishState.Submit);
            }
        }
    }
}