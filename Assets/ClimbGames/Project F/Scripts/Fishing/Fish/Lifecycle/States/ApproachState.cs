using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishLifecycleStateMachine
    {
        public class ApproachState : LifecycleStageBase
        {
            private float biteTime;
            private float elapsedTime;

            public ApproachState(FishingPlayer player, FishLifecycleStateMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 결정: 기본 데이터와 패턴 asset 준비 (지금은 임시 물고기 0 하나)
                Player.PrepareFish(0);

                biteTime = Random.Range(5f, 10f);
                elapsedTime = 0f;
            }

            //protected override void OnUpdate()
            //{
            //    elapsedTime += Time.deltaTime;
            //    if (elapsedTime > biteTime)
            //        fsm.ChangeState(FishLifecycleState.Bite);    // 바늘을 물지는 BiteState 가 biteChance 로 정함
            //}
        }
    }
}