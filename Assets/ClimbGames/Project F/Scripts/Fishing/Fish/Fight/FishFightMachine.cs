using UnityEngine;

namespace ClimbGames.ProjectF
{
    public enum FishFightState
    {
        Run,                // 질주: 플레이어 반대쪽으로 강하게
        SideRun,            // 측면 질주: 줄에 수직으로
        Dive,               // 잠수: 바닥 쪽으로
        HeadShake,          // 머리 흔들기: 제자리에서 좌우로
        Rest,               // 휴식: 힘을 빼고 끌려옴
    }

    // 바늘을 문 물고기(StruggleState)의 저항 패턴
    public partial class FishFightMachine : FSM<FishFightState>
    {
        private const int MaxRepeat = 2;                // 같은 패턴 연속 최대 횟수
        private const float MinRestoreSpeed = 0.5f;     // 끌려오는 정도 계산의 최소 속도 (멈춘 물고기도 끌려옴)
        private const float MinOutward = 0.3f;          // 끌려오는 정도 계산의 최소 바깥 성분

        private FishingPlayer player;
        private HookedFish fish;
        private FishFightPattern pattern;
        private int repeatCount;

        public FishFightStateBase Current => CurrentState as FishFightStateBase;

        public abstract class FishFightStateBase : StateBase
        {
            private NormalizedTimer timer = new NormalizedTimer();

            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;
            protected FishFightMachine Machine => fsm as FishFightMachine;
            protected HookedFish Fish => Machine.fish;
            protected FishFightPattern Pattern => Machine.pattern;
            protected float DepthRatio { get; private set; }
            protected float NormalizedTime => timer.Value;
            public FishFightPattern.StateData Data { get; private set; }

            protected abstract FishFightState State { get; }

            public FishFightStateBase(FishingPlayer player, FishFightMachine fsm) : base(fsm)
            {
                Player = player;
            }

            protected override void OnEnter()
            {
                Restart();
            }

            // 같은 패턴을 다시 시작 (FSM 은 같은 상태로 ChangeState 하지 않음)
            public void Restart()
            {
                Data = Pattern.Get(State);

                float duration = Data != null ? Random.Range(Data.duration.x, Data.duration.y) : 1f;
                timer.Reset(Mathf.Max(0.1f, duration));

                Vector2 depthRange = Data != null ? Data.depthRange : new Vector2(0.2f, 0.5f);
                DepthRatio = Mathf.Clamp01(Random.Range(depthRange.x, depthRange.y));

                OnStart();
            }

            protected override void OnFixedUpdate()
            {
                if (Fish == null)
                    return;

                timer.Update(Time.fixedDeltaTime);
                Fish.SetIntent(BuildIntent());

                if (timer.IsCompleted)
                    Machine.ChangeToNext();
            }

            protected virtual void OnStart() { }

            // 패턴이 원하는 수평 방향 (수역 조향 전)
            protected abstract Vector3 GetDesiredHeading();

            // 플레이어가 이 패턴에 맞게 대응하고 있는지
            public abstract bool IsCountered();

            FishIntentInfo BuildIntent()
            {
                float stamina = Mathf.Max(0.2f/*최소*/, StaminaScale);

                float speed = (Data != null ? Data.speed : 0f) * stamina;
                float pullForce = Mathf.Max(0.05f, (Data != null ? Data.pullForce : 0.5f) * Mathf.Lerp(0.3f, 1f, stamina));

                float lookAhead = Mathf.Max(0.5f, speed * 0.5f);
                Vector3 away = AwayDirection;
                Vector3 heading = Player.Spot.SteerInWater(Fish.transform.position, GetDesiredHeading(), lookAhead, away, Pattern.depth);

                // 장력이 pullForce 가 되도록: 바깥으로 멀어지는 속도 = 줄 힘(N) · gain, 줄 힘 = pullForce · 줄 강도(N)
                // (옆으로 달릴 때도 버티도록 바깥 성분은 최소 MinOutward 로 봄)
                float outward = Mathf.Max(MinOutward, Vector3.Dot(heading, away));
                float restoreGain = Mathf.Max(MinRestoreSpeed, speed * outward) / (pullForce * Mathf.Max(0.1f, Actor.Rod.BreakForce));

                return new FishIntentInfo()
                {
                    state = State,
                    heading = heading,
                    swimSpeed = speed,
                    depthRatio = DepthRatio,
                    pullForce = pullForce,
                    restoreGain = restoreGain,
                    jitter = Data != null ? Data.jitter : 1f,
                };
            }

            // 도움 함수

            protected bool IsReeling => Player.Model.reelingValue > 0f;

            protected float StaminaScale => Player.FishData.staminaCurve.Evaluate(1f - Player.Model.FishStatus.stamina);

            // 플레이어(캐릭터) → 물고기 수평 방향
            protected Vector3 AwayDirection
            {
                get
                {
                    Vector3 away = Fish.transform.position - Actor.transform.position;
                    away.y = 0f;
                    return away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward;
                }
            }

            protected Vector3 FishForward
            {
                get
                {
                    Vector3 forward = Fish.transform.forward;
                    forward.y = 0f;
                    return forward.sqrMagnitude > 0.0001f ? forward.normalized : AwayDirection;
                }
            }

            // away 기준 ±angle 범위에서 수역 여유가 가장 긴 방향
            protected Vector3 FindOpenDirection(Vector3 baseDirection, float angle, int candidates)
            {
                Vector3 position = Fish.transform.position;
                Vector3 best = baseDirection;
                float bestScore = float.MinValue;

                candidates = Mathf.Max(1, candidates);
                for (int i = 0; i < candidates; ++i)
                {
                    float t = candidates > 1 ? (float)i / (candidates - 1) : 0.5f;
                    Vector3 direction = Quaternion.AngleAxis(Mathf.Lerp(-angle, angle, t), Vector3.up) * baseDirection;

                    // 약간의 랜덤으로 매번 같은 방향이 되지 않게
                    float score = Player.Spot.GetFreeDistance(position, direction, Pattern.runLookDistance, Pattern.depth) + Random.Range(0f, 0.5f);
                    if (score > bestScore)
                    {
                        best = direction;
                        bestScore = score;
                    }
                }

                return best;
            }
        }

        public FishFightMachine() : base("<color=cyan>FishFight</color>", true)
        {
        }

        public void InitializeAndStart(FishingPlayer player, HookedFish fish, FishFightPattern pattern)
        {
            this.player = player;
            this.fish = fish;
            this.pattern = pattern;
            repeatCount = 1;

            Initialize(
                (FishFightState.Run, new RunState(player, this)),
                (FishFightState.SideRun, new SideRunState(player, this)),
                (FishFightState.Dive, new DiveState(player, this)),
                (FishFightState.HeadShake, new HeadShakeState(player, this)),
                (FishFightState.Rest, new RestState(player, this)));

            onChange += OnFightStateChanged;

            // 바늘이 박힌 직후 첫 질주
            Start(FishFightState.Run);
        }

        void OnFightStateChanged(FishFightState state)
        {
            player.Model.FishStatus.fightState = state;
            player.MessageBroker.Publish(new FishingEvent.OnFishFightStateChanged() { state = state });
        }

        // 지금 패턴이 끝나면 가중치로 다음 패턴 선택
        public void ChangeToNext()
        {
            FishFightState next = SelectNext();
            ChangeTo(next);
        }

        public void ChangeTo(FishFightState next)
        {
            if (next == CurrentType && CurrentState != null)
            {
                repeatCount++;
                Current?.Restart();
                return;
            }

            repeatCount = 1;
            ChangeState(next);
        }

        FishFightState SelectNext()
        {
            var states = pattern.States;
            if (states == null || states.Length == 0)
                return FishFightState.Rest;

            float x = 1f - player.Model.FishStatus.stamina;
            float total = 0f;
            for (int i = 0; i < states.Length; ++i)
                total += GetWeight(states[i], x);

            if (total <= 0f)
                return FishFightState.Rest;

            float pick = Random.Range(0f, total);
            for (int i = 0; i < states.Length; ++i)
            {
                float weight = GetWeight(states[i], x);
                if (weight <= 0f)
                    continue;

                if (pick < weight)
                    return states[i].state;

                pick -= weight;
            }

            return FishFightState.Rest;
        }

        float GetWeight(FishFightPattern.StateData data, float x)
        {
            if (data == null || data.weightByStamina == null)
                return 0f;

            // 같은 패턴은 연속 MaxRepeat 회까지
            if (CurrentState != null && data.state == CurrentType && repeatCount >= MaxRepeat)
                return 0f;

            return Mathf.Max(0f, data.weightByStamina.Evaluate(x));
        }
    }
}
