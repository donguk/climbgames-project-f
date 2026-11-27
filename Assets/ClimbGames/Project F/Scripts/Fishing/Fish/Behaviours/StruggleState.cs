using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class StruggleState : FishStateBase
        {
            private const float SubmitLineLength = 1f;
            private const float SettleRestoreGain = 4f;     // 끝난 뒤 줄에 끌려오는 정도

            private HookedFish targetFish;
            private FishFightPattern pattern;
            private FishFightMachine fightMachine;

            public StruggleState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 데이터 초기화
                FishingPlayerModel model = Player.Model;
                model.tensionOverTime = 0f;
                model.slackOverTime = 0f;
                model.isDragging = false;

                pattern = Player.FetchFightPattern();
                if (pattern == null)
                {
                    // OnEnter 안에서는 상태를 바꿀 수 없으므로 OnFixedUpdate 에서 Idle 로 돌아감
                    Debug.LogError("[StruggleState] FightPattern/FightPattern_0.asset 을 불러오지 못했습니다.");
                    return;
                }

                // 물고기 생성: 플레이어 반대쪽을 보게
                targetFish = Player.CreateHookedFish(Actor.Hook);
                Vector3 away = targetFish.transform.position - Actor.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    targetFish.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);

                targetFish.SetWater(Player.Spot, pattern.depth, pattern.verticalSpeed);

                Player.MessageBroker.Publish(new FishingEvent.OnFishBite()
                {
                    hookedFish = targetFish
                });

                // 저항 패턴 시작
                fightMachine = new FishFightMachine();
                fightMachine.InitializeAndStart(Player, targetFish, pattern);
            }

            protected override void OnExit()
            {
                fightMachine?.Dispose();
                fightMachine = null;

                // 힘을 빼고 수면 쪽으로, 줄에는 끌려옴 (회수 전까지)
                if (targetFish != null)
                {
                    targetFish.SetIntent(new FishIntentInfo()
                    {
                        state = FishFightState.Rest,
                        heading = targetFish.transform.forward,
                        restoreGain = SettleRestoreGain,
                    });
                }
            }

            protected override void OnFixedUpdate()
            {
                if (pattern == null)
                {
                    fsm.ChangeState(FishState.Idle);
                    return;
                }

                FishingPlayerModel model = Player.Model;
                FishData fishData = Player.FishData;

                // 스태미나: 패턴에 맞게 대응하면 빠르게, 아니면 느리게 소모 (휴식 중 감지 않으면 회복)
                var current = fightMachine?.Current;
                if (current != null)
                {
                    float baseRate = fishData.staminaDuration > 0f ? 1f / fishData.staminaDuration : 0f;
                    bool countered = current.IsCountered();

                    if (fightMachine.CurrentType == FishFightState.Rest && countered == false)
                    {
                        model.FishStatus.RecoverStamina(baseRate * pattern.restRecover * Time.fixedDeltaTime);
                    }
                    else
                    {
                        float cost = current.Data != null ? current.Data.staminaCost : 1f;
                        float multiplier = countered ? pattern.counteredMultiplier : pattern.uncounteredMultiplier;
                        model.FishStatus.DecreaseStamina(baseRate * cost * multiplier * Time.fixedDeltaTime);
                    }
                }

                // 줄 끊김 / 바늘 빠짐
                FishingRod rod = Actor.Rod;
                if (model.tensionOverTime > rod.BreakTime || model.slackOverTime > rod.SlackTime)
                {
                    fsm.ChangeState(FishState.Flee);
                    return;
                }

                // 다 끌어옴
                if (Actor.Line.TotalLength < SubmitLineLength)
                    fsm.ChangeState(FishState.Submit);
            }
        }
    }
}
