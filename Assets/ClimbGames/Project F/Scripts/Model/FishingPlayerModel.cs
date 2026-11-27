using R3;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishStatus
    {
        public float stamina;          // 0~1
        public float noiseSeed;
        public FishFightState fightState;

        public void Initialize()
        {
            stamina = 1f;
            noiseSeed = Random.Range(0f, 1000f);
            fightState = FishFightState.Run;
        }

        public void DecreaseStamina(float value)
        {
            stamina = Mathf.Clamp01(stamina - value);
        }

        public void RecoverStamina(float value)
        {
            stamina = Mathf.Clamp01(stamina + value);
        }
    }

    public class FishingPlayerModel
    {
        private readonly int id;

        public Vector3 characterMovement;
        public float characterPan;
        public float characterTilt;
        public float castingIntensity;
        public float reelingValue;

        public float lineTension;
        public float currentTension;
        public float tensionOverTime;   // 끊김 한계를 넘은 시간
        public float slackOverTime;     // 줄이 느슨한 시간
        public bool isDragging;         // 드랙으로 줄이 풀리는 중

        private ReactiveProperty<float> lineLength = new ReactiveProperty<float>();
        private ReactiveProperty<CastingType> castingType = new ReactiveProperty<CastingType>();
        private ReactiveProperty<FishingActorState> actorState = new ReactiveProperty<FishingActorState>();
        private FishStatus fishStatus = new FishStatus();

        public int ID => id;
        public ReadOnlyReactiveProperty<float> LineLength => lineLength;
        public ReadOnlyReactiveProperty<CastingType> CastingType => castingType;
        public ReadOnlyReactiveProperty<FishingActorState> ActorState => actorState;
        public FishStatus FishStatus => fishStatus;

        public FishingPlayerModel(int id)
        {
            this.id = id;
        }

        public void SetMovement(float horizontal, float vertical)
        {
            characterMovement = new Vector3(horizontal, 0f, vertical);
        }

        public void SetLineLength(float value)
        {
            lineLength.Value = value;
        }

        public void ChangeCastingType(CastingType value)
        {
            castingType.Value = value;
        }

        public void SetCastingIntensity(float intensity)
        {
            castingIntensity = intensity;
        }

        public void SetReelingInput(bool value)
        {
            reelingValue = value ? 1f : 0f;
        }

        public void ChangeActorState(FishingActorState value)
        {
            actorState.Value = value;
        }
    }
}