using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

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
        private FishingActionController actionController = new FishingActionController();

        public Transform CameraTarget => cameraController.TrackingTarget;
        public FishingCameraController CameraController => cameraController;
        public FishingCharacter Character => fishingCharacter;

        protected override void Awake()
        {
            base.Awake();

            inputProvider.CastingInput.dragStart += OnCastingAim;
            inputProvider.CastingInput.dragEnd += OnCastingRelease;
        }

        void FixedUpdate()
        {
            // character
            fishingCharacter.ModelRoot.rotation = cameraController.Pivot.rotation;

            // line
            {
                fishingLine.Simulate(Time.fixedDeltaTime);
                fishingLine.ApplyConstraints();

                if (fishingBait != null)
                    fishingLine.ApplyBaitConstraint(fishingBait);
            }
        }

        public async UniTask Initialize(FishingPoint point)
        {
            fishingPoint = point;
            var eulerAngles = point.CharacterPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(point.CharacterPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));
            cameraController.Initialize();

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", fishingCharacter.RodHandle);

            actionController.InitializeAndStart(this);
            await UniTask.NextFrame();
            fishingLine.BuildLine(fishingRod, 0.5f);

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);
            fishingBait.SetHook(fishingLine.LineEnd);

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void ThrowBait()
        {
            //
            //
            fishingBait.Throw(fishingPoint.CastPosition, 20f);
        }

        void OnCastingAim()
        {


            actionController.ChangeState(FishingAction.CastingAim, new FishingActionController.CastingAimActionParam(inputProvider.CastingInput));
        }

        void OnCastingRelease()
        {
            var param = new FishingActionController.CastingReleaseActionParam(inputProvider.CastingInput);
            param.onThrowBait += ThrowBait;
            actionController.ChangeState(FishingAction.CastingRelease, param);
        }
    }
}