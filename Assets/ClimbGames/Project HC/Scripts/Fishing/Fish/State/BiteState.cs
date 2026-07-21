using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class BiteState : FishStateBase
        {
            private NormalizedTimer biteTimer = new NormalizedTimer();
            private Vector3 angleAxis;

            public BiteState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                biteTimer.Reset(FishData.biteClip.duration);
                angleAxis = Random.Range(0, 2) > 0 ? Vector3.forward : Vector3.back;
            }

            protected override void OnUpdate()
            {
                biteTimer.Update(Time.deltaTime);

                if (Actor.Bobber.IsFloating)
                {
                    Actor.Bobber.SetBiteInfo(new FishBiteInfo()
                    {
                        bobberHeight = FishData.biteClip.bobberHeight.Evaluate(biteTimer.Value),
                        bobberRotation = FishData.biteClip.bobberRotation.Evaluate(biteTimer.Value),
                        angleAxis = angleAxis,
                    });
                }

                if (biteTimer.IsCompleted)
                    fsm.ChangeState(FishState.Struggle);
            }
        }
    }
}