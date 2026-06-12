using R3;

namespace ClimbGames.Project_HC
{
    public class FishingModel : Singleton<FishingModel>
    {
        private ReactiveProperty<CastingType> castingType = new ReactiveProperty<CastingType>();
        private Subject<float> onStartCasting = new Subject<float>();

        public ReadOnlyReactiveProperty<CastingType> CastingType => castingType;
        public Observable<float> OnStartCastingAsObservable() => onStartCasting.AsObservable();

        public void ChangeCastingType(CastingType type)
        {
            castingType.Value = type;
        }

        public void StartCasting(float power)
        {
            onStartCasting.OnNext(power);
        }
    }
}