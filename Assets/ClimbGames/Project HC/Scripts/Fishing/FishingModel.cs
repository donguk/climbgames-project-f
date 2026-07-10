using R3;

namespace ClimbGames.Project_HC
{
    public class FishingModel : Singleton<FishingModel>
    {
        private ReactiveProperty<CastingType> castingType = new ReactiveProperty<CastingType>();
        private Subject<float> onStartCasting = new Subject<float>();
        private ReactiveProperty<FishingActorState> fishingState = new ReactiveProperty<FishingActorState>(FishingActorState.Idle);

        public ReadOnlyReactiveProperty<CastingType> CastingType => castingType;
        public Observable<float> OnStartCastingAsObservable() => onStartCasting.AsObservable();
        public ReadOnlyReactiveProperty<FishingActorState> FishingState => fishingState;
        public bool IsLimitedTilt => castingType.CurrentValue == Project_HC.CastingType.Pitching && fishingState.CurrentValue == FishingActorState.Idle;

        public void ChangeCastingType(CastingType type)
        {
            castingType.Value = type;
        }

        public void StartCasting(float power)
        {
            onStartCasting.OnNext(power);
        }

        public void ChangeFishingState(FishingActorState state)
        {
            fishingState.Value = state;
        }
    }
}