using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class Fish : FSM<FishState>
    {
        private FishData data;
        private FishStatus status;
        private HookedFish hookedFish;

        public FishData Data => data;
        public FishStatus Status => status;
        public HookedFish Target => hookedFish;
        public float CurrentStamina => data.staminaCurve.Evaluate(1f - status.stamina);

        public Fish() : base("<color=blue>Fish</color>", true)
        {
            status = new FishStatus();
        }

        public FishData FetchData()
        {
            if (data == null)
                data = AssetManager.LoadAsset<FishData>("Fish/FishData.asset");

            status.Initialize(data);
            return data;
        }

        public HookedFish CreateHookedFish(FishingHook hook)
        {
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

            if (data != null)
                AssetManager.Release(data);

            hookedFish = null;
            data = null;
        }

        public float GetTensionNoise()
        {
            float currentJitter = Mathf.Max(0.01f, data.tensionJitterIntensity * CurrentStamina);
            float noise = (Mathf.PerlinNoise(Time.time * data.tensionJitterFrequency + status.noiseSeed, 0f) * 2f) - 1f;
            return noise * currentJitter;
        }
    }
}