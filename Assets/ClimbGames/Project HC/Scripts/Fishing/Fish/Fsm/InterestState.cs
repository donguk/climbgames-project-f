using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class InterestState : FishStateBase
        {
            private float biteTime;
            private float progressTime;

            public InterestState(FishingActor actor, FishController fsm) : base(actor, fsm)
            {
            }

            protected override void OnEnter()
            {
                biteTime = Random.Range(5f, 10f);
                progressTime = 0f;
            }

            protected override void OnUpdate()
            {
                progressTime += Time.deltaTime;
                if (progressTime > biteTime)
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