using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingActor")]
    public class FishingActor : MonoSingleton<FishingActor>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private FishingCameraController cameraController;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;

        private FishingPoint fishingPoint;
        private FishingRod fishingRod;
        private FishingBait fishingBait;
        private FishingActorStateController stateController = new FishingActorStateController();
        private float reelingInput;

        public FishingInputProvider Input => inputProvider;
        public Transform CameraTarget => cameraController.TrackingTarget;
        public FishingCameraController CameraController => cameraController;
        public FishingPoint Point => fishingPoint;
        public FishingCharacter Character => fishingCharacter;
        public FishingBait Bait => fishingBait;
        public FishingLine Line => fishingLine;

        void Update()
        {
            // camera pivot
            if (inputProvider.CastingInput.isPressed)
                cameraController.UpdateRotation(inputProvider.CastingInput.delta);

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasReleasedThisFrame)
                    StartCasting();
            }
        }

        void FixedUpdate()
        {
            // character
            fishingCharacter.ModelRoot.rotation = cameraController.Pivot.rotation;

            // line
            fishingLine.Simulate(Time.fixedDeltaTime);
            fishingLine.Constraints();
        }

        public async UniTask Initialize(FishingPoint point)
        {
            fishingPoint = point;

            var eulerAngles = point.CharacterPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(point.CharacterPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));
            cameraController.Initialize();

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", fishingCharacter.RodHandle);

            stateController.InitializeAndStart(this);
            await UniTask.NextFrame();

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);

            // 
            fishingLine.Initialize(fishingRod, 0.5f);
            fishingBait.Attach(fishingLine.End);

            gameObject.SetActive(true);
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