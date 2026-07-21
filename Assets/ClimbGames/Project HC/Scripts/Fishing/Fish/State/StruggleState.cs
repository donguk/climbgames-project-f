using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class StruggleState : FishStateBase
        {
            private HookedFish targetFish;
            private float pullAngleThreshold = Mathf.Cos(30f * Mathf.Deg2Rad);

            public StruggleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 생성
                targetFish = Controller.CreateHookedFish();

                // 바늘 연결
                Actor.Bobber.AttachToLine(Actor.Line.BobberPoint);
                Actor.Line.SetHookedFish(targetFish);
            }

            protected override void OnFixedUpdate()
            {
                float reelingInput = FishingInput.Instance.Reeling;

                // 물고기 스테미나 감소
                Vector3 characterToFish = targetFish.MoveTarget.position - Actor.transform.position;
                characterToFish.y = 0f;

                float pullAngle = Vector3.Dot(characterToFish.normalized, Actor.Rod.transform.forward);
                if (reelingInput > 0f || pullAngle < pullAngleThreshold)
                    Controller.fishStatus.DecreaseStamina(FishData.staminaDecreaseRate * Time.fixedDeltaTime);

                // 스테미나에 따른 이동범위
                float currentStamina = Controller.CurrentStamina;
                float struggleRange = Mathf.Max(0.2f, FishData.moveRange * currentStamina);

                var areaInfo = Actor.WaterArea.GetInnerAreaInfo(targetFish.MoveTarget.position, struggleRange);
                targetFish.SetStruggleInfo(new FishStruggleInfo()
                {
                    areaInfo = areaInfo,
                    currentSpeed = Mathf.Max(0.1f, FishData.moveSpeed * currentStamina),
                    stamina = currentStamina,
                });
            }
        }
    }
}