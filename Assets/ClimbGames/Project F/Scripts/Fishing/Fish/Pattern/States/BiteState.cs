using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class FishPatternStateMachine
    {
        // 입질 클립: 물고기가 바늘을 당김 (찌가 수면에 떠 있을 때만, 찌의 잠김·기울기는 줄 물리가 만듦)
        // 바늘을 문 물고기(HookedFish)가 아직 없어도 동작
        public class BiteState : PatternStateBase
        {
            private Vector3 pullDirection;

            // 생애 단계의 BiteState 와 FSM 로그에서 구분
            public override string Name => "PatternBite";

            protected override FishPatternState State => FishPatternState.Bite;
            BiteClip Data => Clip as BiteClip;

            public BiteState(FishingPlayer player, FishPatternStateMachine fsm) : base(player, fsm)
            {
            }

            // 같은 타입이 이어지면 다시 진입하므로 방향은 여기서 정함
            protected override void OnEnter(ClipParam param)
            {
                base.OnEnter(param);

                pullDirection = GetPullDirection(Data != null ? Data.pullAngle : 70f);
            }

            protected override void OnProgress(float t)
            {
                if (Data != null && Data.pull != null && Actor.Bobber.IsFloating)
                    Actor.Hook.SetPull(pullDirection, Data.pull.Evaluate(t));
                else
                    Actor.Hook.ClearPull();
            }

            protected override void OnExit()
            {
                Actor.Hook.ClearPull();
            }

            // 입질 당김 방향: 랜덤 방위를 pullAngle 만큼 아래로 기울인 단위벡터
            Vector3 GetPullDirection(float pullAngle)
            {
                float azimuth = Random.Range(0f, 2f * Mathf.PI);
                float pitch = pullAngle * Mathf.Deg2Rad;
                Vector3 horizontal = new Vector3(Mathf.Cos(azimuth), 0f, Mathf.Sin(azimuth));
                return horizontal * Mathf.Cos(pitch) + Vector3.down * Mathf.Sin(pitch);
            }
        }
    }
}
