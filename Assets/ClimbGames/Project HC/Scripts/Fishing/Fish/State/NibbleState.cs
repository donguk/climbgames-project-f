using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class NibbleState : FishStateBase
        {
            private NormalizedTimer nibbleTimer = new NormalizedTimer();
            private Vector3 angleAxis;

            public NibbleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                nibbleTimer.Reset(FishData.nibbleClip.duration);
                angleAxis = Random.Range(0, 2) > 0 ? Vector3.forward : Vector3.back;
            }

            protected override void OnUpdate()
            {
                nibbleTimer.Update(Time.deltaTime);

                if (Actor.Bobber.IsFloating)
                {
                    Actor.Bobber.SetBiteInfo(new FishBiteInfo()
                    {
                        bobberHeight = FishData.nibbleClip.bobberHeight.Evaluate(nibbleTimer.Value),
                        bobberRotation = FishData.nibbleClip.bobberRotation.Evaluate(nibbleTimer.Value),
                        angleAxis = angleAxis,
                    });
                }

                if (nibbleTimer.IsCompleted)
                    fsm.ChangeState(FishState.Approach);
            }
        }
    }
}