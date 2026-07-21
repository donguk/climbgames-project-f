using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishState
    {
        Idle,
        Approach,           // 물고기
        Nibble,             // 입질 단계
        Bite,               // 바늘을 물었음
        Struggle,
        Flee,
    }

    public class FishStatus
    {
        public float stamina;          // 0~1
        public float noiseSeed;

        public void Initialize(FishData data)
        {
            stamina = 1f;
            noiseSeed = Random.Range(0f, 1000f);
        }

        public void DecreaseStamina(float value)
        {
            stamina = Mathf.Clamp01(stamina - value);
        }

    }

    public partial class FishController : FSM<FishState>
    {
        public class FishStateBase : StateBase
        {
            protected FishController Controller { get; private set; }
            protected FishingActor Actor => Controller.actor;
            protected FishData FishData => Controller.fishData;
            protected HookedFish HookedFish => Controller.hookedFish;

            public FishStateBase(FishController fsm) : base(fsm)
            {
                Controller = fsm;
            }
        }

        private FishingActor actor;
        private FishData fishData;
        private HookedFish hookedFish;
        private FishStatus fishStatus = new FishStatus();

        public bool IsHooked => hookedFish != null;
        public FishData Data => fishData;
        public HookedFish Target => hookedFish;
        public FishStatus Status => fishStatus;
        public float CurrentStamina => fishData.staminaCurve.Evaluate(1f - fishStatus.stamina);

        public FishController() : base("Fish", true)
        {
        }

        public void InitializeAndStart(FishingActor actor)
        {
            this.actor = actor;

            Initialize(
                (FishState.Idle, new IdleState(this)),
                (FishState.Approach, new ApproachState(this)),
                (FishState.Nibble, new NibbleState(this)),
                (FishState.Bite, new BiteState(this)),
                (FishState.Struggle, new StruggleState(this)));

            Start(FishState.Idle);
        }

        public void FetchData()
        {
            if (fishData == null)
                fishData = AssetManager.LoadAsset<FishData>("Fish/FishData.asset");

            fishStatus.Initialize(fishData);
        }

        public HookedFish CreateHookedFish()
        {
            if (hookedFish == null)
                hookedFish = AssetManager.Instantiate<HookedFish>("HookedFish");

            hookedFish.transform.position = actor.Hook.transform.position;
            hookedFish.Initialize(this);
            return hookedFish;
        }

        public float GetTensionNoise()
        {
            float currentJitter = fishData.tensionJitterIntensity * CurrentStamina;
            float noise = (Mathf.PerlinNoise(Time.time * fishData.tensionJitterFrequency + fishStatus.noiseSeed, 0f) * 2f) - 1f;
            return noise * currentJitter;
        }
    }
}