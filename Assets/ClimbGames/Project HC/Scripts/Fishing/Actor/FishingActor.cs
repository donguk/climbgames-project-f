using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using UnityEngine.InputSystem;

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
        public FishingBobber Bobber => fishingBobber;
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
            FishingModel.Instance.ChangeFishingState(FishingActorState.Idle);
            controller.onChange += (_) =>
            {
                FishingModel.Instance.ChangeFishingState(_);
            };
        }

        void Update()
        {
            if (inputProvider.CastingInput.isPressed)
                fishingCamera.UpdateRotation(inputProvider.CastingInput.delta);

            //
            if (Keyboard.current.upArrowKey.isPressed)
                fishingLine.Reeling(Time.deltaTime * 2f);

            if (Keyboard.current.downArrowKey.isPressed)
                fishingLine.Release(Time.deltaTime * 2f);
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
            fishingHook.Attach(fishingLine.EndPoint);
        }

        public void ChangeCastingType(CastingType type)
        {
            fishingCharacter.SetCastingType(type);
            fishingBobber.SetCastingType(type);
            fishingLine.SetHandleTarget(type == CastingType.Pitching ? fishingCharacter.LeftHandle : null);
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

            fishingLine.CastingThrow(fishingArea.CastPosition, angle);
        }

        public void RetrieveRod()
        {
            if (controller.CurrentType == FishingActorState.Waiting)
            {
                fishingCharacter.SetTrigger(FishingAnimationParam.RetrieveRod);
                fishingLine.SetLength(0.5f);
                //fishingLine.Handle.Retrieve();

                controller.ChangeState(FishingActorState.Idle);
            }
        }
    }
}