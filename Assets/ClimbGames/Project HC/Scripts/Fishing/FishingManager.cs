using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingManager")]
    public class FishingManager : MonoSingleton<FishingManager>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;

        private FishingPoint fishingPoint;
        private FishingRod fishingRod;
        private FishingBait fishingBait;
        private FishingActor fishingActor = new FishingActor();

        public Transform CharacterHead => fishingCharacter.Head;

        protected override void Awake()
        {
            base.Awake();

            inputProvider.CastingInput.dragStart += OnLoadingRod;
            inputProvider.CastingInput.dragEnd += OnCastRod;
        }

        public void Setup(FishingPoint fishingPoint)
        {
            this.fishingPoint = fishingPoint;
            fishingCharacter.Setup(fishingPoint);

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", fishingCharacter.Hand);

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);

            fishingRod.SetBait(fishingBait);
            fishingBait.Setup(fishingRod);
            fishingLine.Setup(fishingRod, fishingBait);

            fishingActor.Initialize(fishingCharacter, fishingRod);
            fishingActor.Start(FishingAction.Idle, fishingRod.GetCancellationTokenOnDestroy());

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void CastBait(float intensity)
        {
            //
            //
            fishingBait.Fire(fishingPoint.CastPosition, 10f);
        }

        void OnLoadingRod()
        {
            fishingActor.ChangeState(FishingAction.CastingAim, new FishingActor.CastingAimActionParam(inputProvider.CastingInput));
        }

        void OnCastRod()
        {
            fishingActor.ChangeState(FishingAction.CastRelease, new FishingActor.CastingReleaseActionParam(inputProvider.CastingInput));
        }
    }
}