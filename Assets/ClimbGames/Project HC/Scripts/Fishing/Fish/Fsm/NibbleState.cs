using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class NibbleState : FishStateBase
        {
            public NibbleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                FishData.nibble.Reset();
                Actor.Bobber.ApplyBiteMotion(FishData.nibble);
            }

            protected override void OnExit()
            {
                Actor.Bobber.ApplyBiteMotion(null);
            }

            protected override void OnUpdate()
            {
                FishData.nibble.Update(Time.deltaTime);

                if (FishData.nibble.IsCompleted)
                    fsm.ChangeState(FishState.Approach);
            }
        }
    }
}