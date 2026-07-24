using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishMachine
    {
        public class ApproachState : FishStateBase
        {
            private float biteTime;
            private float elapsedTime;

            public ApproachState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 결정
                Player.FetchFishData();

                biteTime = Random.Range(5f, 10f);
                elapsedTime = 0f;
            }

            protected override void OnUpdate()
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > biteTime)
                {
                    if (Random.Range(0, 2) > 0)
                    {
                        fsm.ChangeState(FishState.Nibble);
                    }
                    else
                    {
                        fsm.ChangeState(FishState.Bite);
                    }
                }
            }
        }
    }
}