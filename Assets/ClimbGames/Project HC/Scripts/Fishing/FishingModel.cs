using R3;

namespace ClimbGames.Project_HC
{
    public class FishingModel : Singleton<FishingModel>
    {
        private ReactiveProperty<CastingType> castingType = new ReactiveProperty<CastingType>();

        public ReadOnlyReactiveProperty<CastingType> CastingType => castingType;

        public void ChangeCastingType(CastingType type)
        {
            castingType.Value = type;
        }
    }
}