using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class BiteState : FishStateBase
        {
            private FishBiteClip biteClip;
            private NormalizedTimer biteTimer = new NormalizedTimer();
            private Vector3 pullDirection;

            public BiteState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                Player.FetchFishData();
                biteClip = Player.FetchBiteClip();

                biteTimer.Reset(biteClip.duration);
                pullDirection = GetPullDirection(biteClip);
            }

            protected override void OnUpdate()
            {
                biteTimer.Update(Time.deltaTime);

                // 물고기가 바늘을 당김 (찌가 수면에 떠 있을 때만, 찌의 잠김·기울기는 줄 물리가 만듦)
                if (Actor.Bobber.IsFloating)
                    Actor.Hook.SetPull(pullDirection, biteClip.pull.Evaluate(biteTimer.Value));
                else
                    Actor.Hook.ClearPull();

                if (biteTimer.IsCompleted)
                    fsm.ChangeState(FishState.Struggle);
            }

            protected override void OnExit()
            {
                Actor.Hook.ClearPull();
            }
        }
    }
}
