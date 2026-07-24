using Cysharp.Threading.Tasks;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingPlayer : FSM<FishingPlayerState>
    {
        // model
        private int id;
        private CastingType castingType;

        public CastingType CastingType => castingType;
        //

        private FishingActor actor;
        private Fish fish;
        private MessageBroker messageBroker;
        private ReactiveProperty<FishingPlayerState> currentState = new ReactiveProperty<FishingPlayerState>();

        public FishingActor Actor => actor;
        public FishingCamera Camera => actor.Camera;
        public Fish Fish => fish;
        public MessageBroker MessageBroker => messageBroker;
        public ReadOnlyReactiveProperty<FishingPlayerState> State => currentState;

        public FishingPlayer(int id) : base("<color=green>Player</color>", true)
        {
            this.id = id;

            fish = new Fish();
            messageBroker = FishingEvent.GetBroker(id);

            onChange += (_) => currentState.Value = _;
        }

        public virtual async UniTask Initialize(FishingArea area)
        {
            if (actor == null)
                actor = AssetManager.Instantiate<FishingActor>("FishingActor");

            actor.gameObject.SetActive(true);
            await actor.Initialize(id, area);

            fish.InitializeAndStart(this);
            InitializeAndStart(this, fish);
        }

        public void Hide()
        {
            if (actor != null)
                actor.gameObject.SetActive(false);

            fish.Pause();
            Pause();
        }

        public void SetCastingType(CastingType type)
        {
            castingType = type;
            Actor.Character.SetParameter(FishingAnimation.CastingType, (int)type);

            messageBroker.Publish(new FishingEvent.OnCastingTypeChanged() { type = type, leftHandle = Actor.Character.LeftHandle });
        }

        public void GetFish()
        {
            ChangeState(FishingPlayerState.Idle);
            fish.ChangeState(FishState.Idle);
        }
    }
}