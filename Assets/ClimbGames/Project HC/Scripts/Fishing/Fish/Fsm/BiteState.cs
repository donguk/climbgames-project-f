using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class BiteState : FishStateBase
        {
            public BiteState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                FishData.bite.Reset();
                Actor.Bobber.ApplyBiteMotion(FishData.bite);
            }

            protected override void OnExit()
            {
                Actor.Bobber.ApplyBiteMotion(null);
            }

            protected override void OnUpdate()
            {
                FishData.bite.Update(Time.deltaTime);

                if (FishData.bite.IsCompleted)
                    fsm.ChangeState(FishState.Struggle);
            }
        }
    }
}