using UnityEngine;

namespace ClimbGames.ProjectF
{
    public enum FishPatternState
    {
        Run,                // 질주: 플레이어 반대쪽으로 강하게
        SideRun,            // 측면 질주: 줄에 수직으로
        Dive,               // 잠수: 바닥 쪽으로
        HeadShake,          // 머리 흔들기: 제자리에서 좌우로
        Rest,               // 휴식: 힘을 빼고 끌려옴
        Bite,               // 입질: 바늘을 당김 (맨 끝에 둠: 앞에 넣으면 기본값이 바뀜)
    }

    // 패턴 상태에 넘기는 클립
    public readonly struct ClipParam : FSM.IStateParam
    {
        public readonly FishPatternClip clip;

        public ClipParam(FishPatternClip clip)
        {
            this.clip = clip;
        }
    }

    // FishPattern 의 클립 시퀀스를 실행하는 상태 머신 (FishPattern 이 하나를 만들어 물고기마다 재사용)
    // - 상태 인스턴스는 타입마다 하나, 클립은 ClipParam 으로 진입할 때 받음
    // - 같은 타입 클립이 이어지면 같은 상태로 다시 진입 (restartIfSame, onChange 는 타입이 바뀔 때만)
    // - 다음 클립은 FishPattern 이 고름 (peek → Commit)
    public partial class FishPatternStateMachine : FSM<FishPatternState>
    {
        private FishingPlayer player;
        private HookedFish fish;
        private FishModel model;
        private FishPattern owner;
        private int repeatCount;                    // 같은 타입 연속 횟수 (Random 모드 연속 제한용)

        public PatternStateBase Current => CurrentState as PatternStateBase;

        // 모든 클립 공통: 클립, 재생 타이머
        public abstract class PatternStateBase : StateBase<ClipParam>
        {
            private NormalizedTimer timer = new NormalizedTimer();

            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;
            protected FishPatternStateMachine Machine => fsm as FishPatternStateMachine;
            protected FishModel Model => Machine.model;
            protected FishSpec Spec => Machine.model.Spec;

            public FishPatternClip Clip { get; private set; }

            protected abstract FishPatternState State { get; }

            public PatternStateBase(FishingPlayer player, FishPatternStateMachine fsm) : base(fsm)
            {
                Player = player;
            }

            // 클립 시작: 다른 타입에서 들어올 때도, 같은 타입을 다시 시작할 때도 여기로
            // 하위 상태는 override 해서 첫 줄에서 base.OnEnter(param) 를 부르고 (Clip, 타이머 준비) 자기 시작 값을 정함
            protected override void OnEnter(ClipParam param)
            {
                Clip = param.clip;
                timer.Reset(Clip != null ? Clip.duration.Pick() : 1f);
            }

            // 순서 고정: 타이머 → OnProgress → 끝났으면 다음 클립 (Advance 는 항상 마지막)
            protected sealed override void OnFixedUpdate()
            {
                timer.Update(Time.fixedDeltaTime);
                OnProgress(timer.Value);

                // OnProgress 에서 다른 클립으로 바뀌었으면 (Rest 탈출 등) 무시
                if (Machine.CurrentState != this || timer.IsCompleted == false)
                    return;

                Machine.Advance();
            }

            // 매 스텝, t = 클립 진행률 0~1 (타이머 갱신 뒤)
            protected virtual void OnProgress(float t) { }
        }

        public FishPatternStateMachine() : base("<color=cyan>FishPattern</color>", true)
        {
        }

        // 처음 한 번: 상태 등록, 이벤트 구독
        public void Setup(FishingPlayer player, FishPattern owner)
        {
            this.player = player;
            this.owner = owner;

            Initialize(
                (FishPatternState.Run, new RunState(player, this)),
                (FishPatternState.SideRun, new SideRunState(player, this)),
                (FishPatternState.Dive, new DiveState(player, this)),
                (FishPatternState.HeadShake, new HeadShakeState(player, this)),
                (FishPatternState.Rest, new RestState(player, this)),
                (FishPatternState.Bite, new BiteState(player, this)));

            onChange += OnPatternTypeChanged;
        }

        // 시퀀스마다: 물고기와 데이터를 정하고 첫 클립으로 시작 (fish 는 입질 중이면 null)
        public void Play(HookedFish fish, FishModel model, FishPatternClip first)
        {
            this.fish = fish;
            this.model = model;
            repeatCount = 1;

            Start(first.Type, new ClipParam(first));
        }

        void OnPatternTypeChanged(FishPatternState state)
        {
            model.SetPatternType(state);
            player.MessageBroker.Publish(new FishingEvent.OnFightPatternChanged() { state = state });
        }

        // 지금 클립이 끝났을 때: 다음 클립으로, 없으면 (Once 끝) 정지
        void Advance()
        {
            if (owner.TryPeekNext(repeatCount, model.Stamina, out var next, out int nextIndex))
            {
                owner.Commit(nextIndex);
                ChangeTo(next);
            }
            else
            {
                owner.Finish();
            }
        }

        // 지금 클립을 일찍 끝냄 (Rest 탈출): 다른 타입 클립이 있을 때만
        // peek 라서 조건이 이어지는 동안 매 스텝 불려도 시퀀스 위치가 넘어가지 않음
        public bool BreakOut()
        {
            if (owner.TryPeekBreakOut(model.Stamina, out var next, out int nextIndex) == false)
                return false;

            owner.Commit(nextIndex);
            ChangeTo(next);
            return true;
        }

        void ChangeTo(FishPatternClip next)
        {
            if (next == null)
                return;

            repeatCount = next.Type == CurrentType && CurrentState != null ? repeatCount + 1 : 1;
            ChangeState(next.Type, new ClipParam(next), restartIfSame: true);
        }
    }
}
