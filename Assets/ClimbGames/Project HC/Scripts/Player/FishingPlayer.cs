using UnityEngine;
using Cysharp.Threading.Tasks;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingPlayer
    {
        private FishingPlayerModel model;
        private FishData fishData;

        private FishingActor actor;
        private HookedFish fish;

        private FishingActorMachine actorMachine;
        private FishMachine fishMachine;
        private MessageBroker messageBroker;

        public FishingPlayerModel Model => model;
        public FishData FishData => fishData;

        public FishingActor Actor => actor;
        public FishingCamera Camera => actor.Camera;
        public HookedFish Fish => fish;
        public MessageBroker MessageBroker => messageBroker;

        public FishingPlayer(int id)
        {
            model = new FishingPlayerModel(id);
            messageBroker = FishingEvent.GetBroker(id);

            actorMachine = new FishingActorMachine();
            fishMachine = new FishMachine();

            actorMachine.onChange += (_) => model.ChangeActorState(_);
        }

        public virtual async UniTask Initialize(FishingArea area)
        {
            if (actor == null)
                actor = AssetManager.Instantiate<FishingActor>("FishingActor");

            actor.gameObject.SetActive(true);
            await actor.Initialize(model.ID, area);

            actorMachine.InitializeAndStart(this);
            fishMachine.InitializeAndStart(this);
        }

        public void Hide()
        {
            if (actor != null)
                actor.gameObject.SetActive(false);

            fishMachine.Pause();
            actorMachine.Pause();
        }

        public void StartCasting(float value)
        {
            messageBroker.Publish(new FishingEvent.OnCastingStarted() { intensity = value });
        }

        // 획득
        public void TakeHookedFish()
        {
            actorMachine.ChangeState(FishingActorState.Idle);
            fishMachine.ChangeState(FishState.Idle);
        }

        public FishData FetchFishData()
        {
            if (fishData == null)
                fishData = AssetManager.LoadAsset<FishData>("Fish/FishData.asset");

            return fishData;
        }

        public HookedFish CreateHookedFish(FishingHook hook)
        {
            model.FishStatus.Initialize();

            if (fish == null)
                fish = AssetManager.Instantiate<HookedFish>("HookedFish");

            fish.transform.position = hook.transform.position;
            fish.Initialize();

            return fish;
        }

        public void DeleteHookedFish()
        {
            if (fish != null)
                GameObject.Destroy(fish.gameObject);

            if (fishData != null)
                AssetManager.Release(fishData);

            fish = null;
            fishData = null;
        }

        public float GetFishNoise()
        {
            float stamina = fishData.staminaCurve.Evaluate(1f - model.FishStatus.stamina);
            float currentJitter = Mathf.Max(0.01f, fishData.tensionJitterIntensity * stamina);
            float noise = (Mathf.PerlinNoise(Time.time * fishData.tensionJitterFrequency + model.FishStatus.noiseSeed, 0f) * 2f) - 1f;
            return noise * currentJitter;
        }
    }
}