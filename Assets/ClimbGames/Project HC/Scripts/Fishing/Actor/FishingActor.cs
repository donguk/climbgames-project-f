using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingActor")]
    public class FishingActor : MonoSingleton<FishingActor>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private FishingCamera fishingCamera;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;

        private FishingArea fishingArea;
        private FishingRod fishingRod;
        private FishingActorStateController stateController = new FishingActorStateController();

        public FishingInputProvider Input => inputProvider;
        public FishingCamera Camera => fishingCamera;
        public FishingArea Area => fishingArea;
        public FishingCharacter Character => fishingCharacter;
        public FishingLine Line => fishingLine;
        public FishingActorState State => stateController.CurrentType;

        void Start()
        {
            FishingModel.Instance.OnStartCastingAsObservable().Subscribe(_ => StartCasting()).AddTo(this);
        }

        void Update()
        {
            if (inputProvider.CastingInput.isPressed)
                fishingCamera.UpdateRotation(inputProvider.CastingInput.delta);
        }

        public async UniTask Initialize(FishingArea area)
        {
            fishingArea = area;

            var eulerAngles = area.CharacterPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(area.CharacterPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));
            fishingCamera.Initialize();

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", fishingCharacter.RodHandle);

            //if (fishingBait == null)
            //    fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);

            // 
            fishingLine.Initialize(fishingRod.PoleTip, 0.5f, 0.25f);
            fishingBobber.Attach(fishingLine.BobberPoint);
            fishingHook.Attach(fishingLine.HookPoint);

            gameObject.SetActive(true);
            await UniTask.NextFrame();
            stateController.InitializeAndStart(this);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void StartCasting()
        {
            if (stateController.CurrentType == FishingActorState.Idle)
                stateController.ChangeState(FishingActorState.Casting);
        }
    }
}