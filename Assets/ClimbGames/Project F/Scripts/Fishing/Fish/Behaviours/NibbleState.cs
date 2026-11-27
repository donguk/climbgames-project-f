using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishMachine
    {
        public class NibbleState : FishStateBase
        {
            private FishBiteClip biteClip;
            private NormalizedTimer nibbleTimer = new NormalizedTimer();
            private Vector3 pullDirection;

            public NibbleState(FishingPlayer player, FishMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                biteClip = Player.FetchBiteClip();
                nibbleTimer.Reset(biteClip.duration);
                pullDirection = GetPullDirection(biteClip);
            }

            protected override void OnUpdate()
            {
                nibbleTimer.Update(Time.deltaTime);

                // 물고기가 바늘을 당김 (찌가 수면에 떠 있을 때만, 찌의 잠김·기울기는 줄 물리가 만듦)
                if (Actor.Bobber.IsFloating)
                    Actor.Hook.SetPull(pullDirection, biteClip.pull.Evaluate(nibbleTimer.Value));
                else
                    Actor.Hook.ClearPull();

                if (nibbleTimer.IsCompleted)
                    fsm.ChangeState(FishState.Idle);
            }

            protected override void OnExit()
            {
                Actor.Hook.ClearPull();
            }
        }

        // 입질 당김 방향: 랜덤 방위를 pullAngle 만큼 아래로 기울인 단위벡터
        static Vector3 GetPullDirection(FishBiteClip clip)
        {
            float azimuth = Random.Range(0f, 2f * Mathf.PI);
            float pitch = clip.pullAngle * Mathf.Deg2Rad;
            Vector3 horizontal = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
            return horizontal * Mathf.Cos(pitch) + Vector3.down * Mathf.Sin(pitch);
        }
    }
}
