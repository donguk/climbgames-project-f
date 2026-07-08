using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;
using UnityEngine.InputSystem;
using NUnit.Framework;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingActor")]
    public class FishingActor : MonoBehaviour
    {
        [SerializeField] private FishingCamera fishingCamera;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;

        private FishingArea fishingArea;
        private FishingRod fishingRod;
        private FishingActorController controller = new FishingActorController();

        public FishingCamera Camera => fishingCamera;
        public FishingArea Area => fishingArea;
        public FishingCharacter Character => fishingCharacter;
        public FishingRod Rod => fishingRod;
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
            controller.onChange += (_) => FishingModel.Instance.ChangeFishingState(_);
        }

        void Update()
        {
            if (FishingInput.Instance.IsPressed)
                fishingCamera.UpdateRotation(FishingInput.Instance.DragDelta);

            // Test
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

            // 채비 설정
            fishingBobber.AttachToLine(fishingLine.BobberPoint);
            fishingHook.AttachToLine(fishingLine.EndPoint);

            await UniTask.Delay(100);
            controller.InitializeAndStart(this);
        }

        public void ChangeCastingType(CastingType type)
        {
            fishingCharacter.SetCastingType(type);
            fishingBobber.SetCastingType(type);
            fishingLine.SetCastingType(type, fishingCharacter.LeftHandle);
        }

        void CastingRod()
        {
            if (controller.CurrentType == FishingActorState.Idle)
                controller.ChangeState(FishingActorState.Casting);
        }

        public void RetrieveRod()
        {
            if (controller.CurrentType == FishingActorState.Waiting)
            {
                fishingCharacter.SetTrigger(FishingAnimationParam.RetrieveRod);
                fishingLine.SetLength(0.5f);
                fishingBobber.AttachToLine(fishingLine.BobberPoint);
                fishingHook.AttachToLine(fishingLine.EndPoint);

                controller.ChangeState(FishingActorState.Idle);
            }
        }
    }
}