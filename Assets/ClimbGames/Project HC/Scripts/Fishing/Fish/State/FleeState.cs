using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class FleeState : FishStateBase
        {
            public FleeState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                FishingEvent.Broker.Publish(new FishingEvent.OnFishFlee());

                // 삭제
                Controller.DeleteHookedFish();

                fsm.ChangeState(FishState.Idle);
            }
        }
    }
}