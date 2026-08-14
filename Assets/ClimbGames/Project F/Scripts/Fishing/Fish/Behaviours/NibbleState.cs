using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class NibbleState : FishStateBase
        {
            private FishData.BiteClip biteClip;
            private NormalizedTimer nibbleTimer = new NormalizedTimer();
            private Vector3 angleAxis;

            public NibbleState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                biteClip = Player.FishData.nibbleClip;
                nibbleTimer.Reset(biteClip.duration);
                angleAxis = Random.Range(0, 2) > 0 ? Vector3.forward : Vector3.back;
            }

            protected override void OnUpdate()
            {
                nibbleTimer.Update(Time.deltaTime);

                if (Actor.Bobber.IsFloating)
                {
                    Actor.Bobber.SetBiteInfo(new FishBiteInfo()
                    {
                        bobberHeight = biteClip.bobberHeight.Evaluate(nibbleTimer.Value),
                        bobberRotation = biteClip.bobberRotation.Evaluate(nibbleTimer.Value),
                        angleAxis = angleAxis,
                    });
                }

                if (nibbleTimer.IsCompleted)
                    fsm.ChangeState(FishState.Approach);
            }
        }
    }
}