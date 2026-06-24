using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingActor")]
    public class FishingActor : MonoBehaviour
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private FishingCamera fishingCamera;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;

        private FishingArea fishingArea;
        private FishingRod fishingRod;
        private FishingActorController controller = new FishingActorController();

        public FishingInputProvider Input => inputProvider;
        public FishingCamera Camera => fishingCamera;
        public FishingArea Area => fishingArea;
        public FishingCharacter Character => fishingCharacter;
        public FishingLine Line => fishingLine;
        public FishingHook Hook => fishingHook;
        public FishingActorState State => controller.CurrentType;

        void OnEnable()
        {
            controller.Resume();
        }

        void OnDisable()
        {
            controller.Pause();
        }

        void Start()
        {
            FishingModel.Instance.OnStartCastingAsObservable().Subscribe(_ => CastingRod()).AddTo(this);
            controller.onChange += (_) => FishingModel.Instance.ChangeFishingState(_);
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

            // 라인 생성
            fishingLine.Initialize(fishingRod.PoleTip, 0.5f, 0.25f);
            controller.InitializeAndStart(this);
            await UniTask.Delay(100);

            // 채비 설정
            fishingBobber.Attach(fishingLine.BobberPoint);
            fishingHook.Attach(fishingLine.HookPoint);
        }

        public void ChangeCastingType(CastingType type)
        {
            fishingCharacter.SetCastingType(type);
            fishingLine.SetHandleTarget(type == CastingType.Pitching ? fishingCharacter.LeftHandle : null);
            fishingBobber.SetCastingType(type);
            fishingHook.SetCastingType(type);
        }

        void CastingRod()
        {
            if (controller.CurrentType == FishingActorState.Idle)
                controller.ChangeState(FishingActorState.Casting);
        }

        public void CastingThrow()
        {
            CastingType castingType = FishingModel.Instance.CastingType.CurrentValue;
            float angle = castingType == CastingType.OverHead ? 45f : 20f;

            fishingLine.Handle.Throw(fishingArea.CastPosition, angle);
            fishingHook.Throw();
        }

        public void RetrieveRod()
        {
            if (controller.CurrentType == FishingActorState.Waiting)
            {
                fishingCharacter.SetTrigger(FishingAnimationParam.RetrieveRod);
                fishingLine.SetLength(0.5f);
                fishingLine.Handle.Retrieve();
                fishingHook.Attach(fishingLine.HookPoint);
                controller.ChangeState(FishingActorState.Idle);
            }
        }
    }
}