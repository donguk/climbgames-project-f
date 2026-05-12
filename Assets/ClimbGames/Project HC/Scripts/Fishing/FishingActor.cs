using Cysharp.Threading.Tasks;
using UnityEditor;
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
        private FishingActionController actionController = new FishingActionController();

        bool isCasted;
        private float reelingInput;

        public Transform CameraTarget => cameraController.TrackingTarget;
        public FishingCameraController CameraController => cameraController;
        public FishingCharacter Character => fishingCharacter;

        protected override void Awake()
        {
            base.Awake();

            inputProvider.CastingInput.dragStart += OnCastingAim;
            inputProvider.CastingInput.dragEnd += OnCastingRelease;
        }

        void Update()
        {
            if (Mouse.current != null)
                reelingInput = Mouse.current.rightButton.isPressed ? 1f : 0f;
        }

        void FixedUpdate()
        {
            // character
            fishingCharacter.ModelRoot.rotation = cameraController.Pivot.rotation;

            // line
            fishingLine.Simulate(Time.fixedDeltaTime);
            fishingLine.Constraints();

            if (fishingBait != null)
            {
                switch (fishingBait.State)
                {
                    case BaitState.Casting:
                        {
                            fishingLine.Release(fishingBait.Velocity.magnitude * Time.fixedDeltaTime * 1.1f);
                            fishingLine.PinBait(fishingBait);
                            break;
                        }
                    case BaitState.Located:
                        {
                            fishingLine.Reeling(reelingInput * Time.fixedDeltaTime);

                            float tensionIntensity = fishingLine.CalculateTension(fishingBait);
                            if (tensionIntensity > 0f)
                                fishingBait.Drag(fishingLine.End.up, tensionIntensity);

                            if (fishingLine.Length < 1f && (fishingBait.LocationFlag == 0 || tensionIntensity > 0.2f))
                            {
                                Debug.Log($"[FishingActor] Suspend Bait: lineLength({fishingLine.Length})/ baitLocation({fishingBait.LocationFlag})/ tensionIntensity({tensionIntensity})");
                                fishingBait.Connect(fishingLine.End);
                            }
                            else
                            {
                                fishingLine.PinBait(fishingBait);
                            }
                            break;
                        }
                }
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

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);

            // 
            fishingLine.Initialize(fishingRod, 0.5f);
            fishingBait.Connect(fishingLine.End);

            gameObject.SetActive(true);
            isCasted = false;
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void CastBait()
        {
            fishingBait.Cast(fishingPoint.CastPosition, 20f);
            fishingLine.CastBait();
        }

        void OnCastingAim()
        {
            actionController.ChangeState(FishingAction.CastingAim, new FishingActionController.CastingAimActionParam(inputProvider.CastingInput));
        }

        void OnCastingRelease()
        {
            if (isCasted)
                return;

            isCasted = true;

            var param = new FishingActionController.CastingReleaseActionParam(inputProvider.CastingInput);
            param.onThrowBait += CastBait;
            actionController.ChangeState(FishingAction.CastingRelease, param);
        }
    }
}