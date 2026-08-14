using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class BiteState : FishStateBase
        {
            private FishData fishData;
            private NormalizedTimer biteTimer = new NormalizedTimer();
            private Vector3 angleAxis;

            public BiteState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                fishData = Player.FetchFishData();

                biteTimer.Reset(fishData.biteClip.duration);
                angleAxis = Random.Range(0, 2) > 0 ? Vector3.forward : Vector3.back;
            }

            protected override void OnUpdate()
            {
                biteTimer.Update(Time.deltaTime);

                if (Actor.Bobber.IsFloating)
                {
                    Actor.Bobber.SetBiteInfo(new FishBiteInfo()
                    {
                        bobberHeight = fishData.biteClip.bobberHeight.Evaluate(biteTimer.Value),
                        bobberRotation = fishData.biteClip.bobberRotation.Evaluate(biteTimer.Value),
                        angleAxis = angleAxis,
                    });
                }

                if (biteTimer.IsCompleted)
                    fsm.ChangeState(FishState.Struggle);
            }
        }
    }
}