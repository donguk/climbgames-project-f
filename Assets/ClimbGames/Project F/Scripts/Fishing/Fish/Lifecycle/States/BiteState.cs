using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishLifecycleStateMachine
    {
        // 입질: 입질 시퀀스를 한 번(Once) 재생하고, 끝나면 확률로 바늘을 완전히 물지(Fight) 놓을지(Idle) 정함
        public class BiteState : LifecycleStageBase
        {
            public BiteState(FishingPlayer player, FishLifecycleStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                FishModel fish = Player.Model.Fish;

                // 물고기 데이터와 클립은 ApproachState 에서 준비됨 (없으면 한 번 더 시도)
                if (fish.Spec == null || Pattern.HasClips == false)
                    Player.PrepareFish(0);

                bool playing = fish.Spec != null
                    && Pattern.Play<BiteClip>(fish.Spec.biteClips, FishPatternEndMode.Once, Player, null, fish);

                if (playing == false)
                {
                    // OnEnter 안의 전환은 이 진입이 끝난 뒤 실행됨
                    Debug.LogError("[BiteState] 입질 클립이 없어 Idle 로 돌아갑니다.");
                    fsm.ChangeState(FishLifecycleState.Idle);
                }
            }

            protected override void OnUpdate()
            {
                if (Pattern.IsFinished == false)
                    return;

                FishSpec spec = Player.Model.Fish.Spec;
                bool hooked = Random.value < spec.biteChance;
                fsm.ChangeState(hooked ? FishLifecycleState.Fight : FishLifecycleState.Idle);
            }

            protected override void OnExit()
            {
                Pattern.Stop();
                Actor.Hook.ClearPull();
            }
        }
    }
}
