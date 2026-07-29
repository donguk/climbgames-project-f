using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public class FishingPlayer
    {
        private FishingPlayerModel model;
        private FishData fishData;

        private FishingSpot spot;
        private FishingActor actor;
        private HookedFish hookedFish;

        private FishingActorMachine actorMachine;
        private FishMachine fishMachine;
        private MessageBroker messageBroker;
        private FishingInputHandler handler;

        public FishingPlayerModel Model => model;
        public FishData FishData => fishData;

        public FishingWaterArea WaterArea => spot.WaterArea;
        public FishingActor Actor => actor;
        public Transform CameraTarget => actor.Character.CameraTarget;
        public HookedFish Fish => hookedFish;
        public FishMachine FishMachine => fishMachine;
        public MessageBroker MessageBroker => messageBroker;

        public FishingPlayer(int id)
        {
            model = new FishingPlayerModel(id);
            messageBroker = FishingEvent.GetBroker(id);

            actorMachine = new FishingActorMachine();
            fishMachine = new FishMachine();

            actorMachine.onChange += (_) => model.ChangeActorState(_);
        }

        public virtual async UniTask Initialize(FishingSpot spot)
        {
            this.spot = spot;

            if (actor == null)
                actor = AssetManager.Instantiate<FishingActor>("FishingActor");

            if (handler == null)
                handler = AssetManager.Instantiate<FishingInputHandler>("FishingInputHandler");

            actor.gameObject.SetActive(true);
            actor.Initialize(model.ID, spot.SpawnPoint);

            actorMachine.InitializeAndStart(this);
            fishMachine.InitializeAndStart(this);

            await UniTask.NextFrame();
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

            if (hookedFish == null)
                hookedFish = AssetManager.Instantiate<HookedFish>("HookedFish");

            hookedFish.transform.position = hook.transform.position;
            hookedFish.Initialize();

            return hookedFish;
        }

        public void DeleteHookedFish()
        {
            if (hookedFish != null)
                GameObject.Destroy(hookedFish.gameObject);

            if (fishData != null)
                AssetManager.Release(fishData);

            hookedFish = null;
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