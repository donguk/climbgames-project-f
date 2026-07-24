using R3;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishStatus
    {
        public float stamina;          // 0~1
        public float noiseSeed;

        public void Initialize()
        {
            stamina = 1f;
            noiseSeed = Random.Range(0f, 1000f);
        }

        public void DecreaseStamina(float value)
        {
            stamina = Mathf.Clamp01(stamina - value);
        }
    }

    public class FishingPlayerModel
    {
        private readonly int id;
        public float lineTension;
        public float currentTension;
        public float tensionOverTime;

        private ReactiveProperty<CastingType> castingType = new ReactiveProperty<CastingType>();
        private ReactiveProperty<FishingActorState> actorState = new ReactiveProperty<FishingActorState>();
        private FishStatus fishStatus = new FishStatus();

        public int ID => id;
        public ReadOnlyReactiveProperty<CastingType> CastingType => castingType;
        public ReadOnlyReactiveProperty<FishingActorState> ActorState => actorState;
        public FishStatus FishStatus => fishStatus;

        public FishingPlayerModel(int id)
        {
            this.id = id;
        }

        public void ChangeCastingType(CastingType value)
        {
            castingType.Value = value;
        }

        public void ChangeActorState(FishingActorState value)
        {
            actorState.Value = value;
        }
    }
}