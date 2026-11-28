using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishLifecycleStateMachine
    {
        // 파이팅: 바늘을 문 물고기를 만들고 파이팅 시퀀스(spec.fightClips)를 실행
        // 스태미나, 줄 끊김·바늘 빠짐, 다 끌어옴 판정
        public class FightState : LifecycleStageBase
        {
            private const float SubmitLineLength = 1f;
            private const float SettleRestoreGain = 4f;     // 끝난 뒤 줄에 끌려오는 정도

            private HookedFish hookedFish;

            public FightState(FishingPlayer player, FishLifecycleStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 데이터 초기화
                hookedFish = null;
                FishingPlayerModel model = Player.Model;
                model.tensionOverTime = 0f;
                model.slackOverTime = 0f;
                model.isDragging = false;

                // 물고기 데이터와 클립은 ApproachState 에서 준비됨 (없으면 한 번 더 시도)
                FishModel fish = model.Fish;
                if (fish.Spec == null || Pattern.HasClips == false)
                    Player.PrepareFish(0);

                // 물고기를 만들기 전에 파이팅 클립이 있는지 확인 (입질 클립만 있으면 시작하지 않음)
                FishSpec spec = fish.Spec;
                if (spec == null || Pattern.HasAny<FightClip>(spec.fightClips) == false)
                {
                    // OnEnter 안의 전환은 이 진입이 끝난 뒤 실행됨
                    Debug.LogError("[FightState] 파이팅 클립이 없어 Idle 로 돌아갑니다.");
                    fsm.ChangeState(FishLifecycleState.Idle);
                    return;
                }

                // 물고기 생성: 플레이어 반대쪽을 보게
                hookedFish = Player.CreateHookedFish(Actor.Hook);
                Vector3 away = hookedFish.transform.position - Actor.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.0001f)
                    hookedFish.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);

                hookedFish.SetWater(Player.Spot, spec.Depth, spec.verticalSpeed);

                Player.MessageBroker.Publish(new FishingEvent.OnFishBite()
                {
                    hookedFish = hookedFish
                });

                // 파이팅 시퀀스 시작 (파이팅은 끝나는 시점이 판정으로 정해지므로 Once 는 쓰지 않고 Loop 로)
                FishPatternEndMode mode = spec.fightEndMode;
                if (mode == FishPatternEndMode.Once)
                {
                    Debug.LogWarning($"[FightState] 파이팅 시퀀스는 Once 를 쓸 수 없어 Loop 로 실행합니다. (fish {spec.id})");
                    mode = FishPatternEndMode.Loop;
                }

                if (Pattern.Play<FightClip>(spec.fightClips, mode, Player, hookedFish, fish) == false)
                {
                    // 만든 물고기는 Idle 의 DeleteHookedFish 가 정리
                    Debug.LogError("[FightState] 파이팅 시퀀스가 비어 Idle 로 돌아갑니다.");
                    fsm.ChangeState(FishLifecycleState.Idle);
                }
            }

            protected override void OnExit()
            {
                Pattern.Stop();

                // 힘을 빼고 수면 쪽으로, 줄에는 끌려옴 (회수 전까지)
                if (hookedFish != null)
                {
                    hookedFish.SetIntent(new FishIntentInfo()
                    {
                        state = FishPatternState.Rest,
                        heading = hookedFish.transform.forward,
                        restoreGain = SettleRestoreGain,
                    });
                }
            }

            protected override void OnFixedUpdate()
            {
                FishingPlayerModel model = Player.Model;
                FishModel fish = model.Fish;
                FishSpec spec = fish.Spec;

                // 스태미나: 패턴에 맞게 대응하면 빠르게, 아니면 느리게 소모 (휴식 중 감지 않으면 회복)
                var current = Pattern.CurrentState as FishPatternStateMachine.FightPatternStateBase;
                var clip = Pattern.Current as FightClip;
                if (current != null)
                {
                    float baseRate = spec.staminaDuration > 0f ? 1f / spec.staminaDuration : 0f;
                    bool countered = current.IsCountered();

                    if (clip != null && clip.Type == FishPatternState.Rest && countered == false)
                    {
                        fish.RecoverStamina(baseRate * spec.restRecover * Time.fixedDeltaTime);
                    }
                    else
                    {
                        float cost = clip != null ? clip.staminaCost : 1f;
                        float multiplier = countered ? spec.counteredMultiplier : spec.uncounteredMultiplier;
                        fish.DecreaseStamina(baseRate * cost * multiplier * Time.fixedDeltaTime);
                    }
                }

                // 줄 끊김 / 바늘 빠짐
                FishingRod rod = Actor.Rod;
                if (model.tensionOverTime > rod.BreakTime || model.slackOverTime > rod.SlackTime)
                {
                    fsm.ChangeState(FishLifecycleState.Flee);
                    return;
                }

                // 다 끌어옴
                if (Actor.Line.TotalLength < SubmitLineLength)
                    fsm.ChangeState(FishLifecycleState.Submit);
            }
        }
    }
}
