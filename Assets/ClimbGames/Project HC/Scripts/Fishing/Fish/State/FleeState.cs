using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class FleeState : FishStateBase
        {
            private float elapsedTime;

            public FleeState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Line.EndPoint.SetTrackingTarget(null);
                Actor.Line.SetForcedTension(0f, 0.1f);
                Actor.Bobber.DetachFromLine();

                // 삭제
                Controller.DeleteHookedFish();

                elapsedTime = 0f;
            }

            protected override void OnUpdate()
            {
                elapsedTime += Time.deltaTime;
                if (elapsedTime > 8f)
                    fsm.ChangeState(FishState.Approach);
            }
        }
    }
}