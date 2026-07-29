using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class FishingInputHandler : MonoBehaviour
    {
        [SerializeField] private float sensitivity = 0.04f;
        [SerializeField] private Vector2 tiltLimit = new Vector2(-30f, 30f);
        [SerializeField] private Vector2 pitchingTiltLimit = new Vector2(-15f, 15f);

        private InputAction touchAction;
        private InputAction touchPosition;
        private InputAction touchDelta;
        private FishingPlayerModel playerModel;

        private bool isPressed;
        private float rawTilt;
        private Vector2 currentTiltLimit;
        private CinemachinePanTilt cameraPanTilt;
        private float cameraStartTilt;

        private bool IsPitchingMode => playerModel != null &&
                                        playerModel.CastingType.CurrentValue == CastingType.Pitching &&
                                        playerModel.ActorState.CurrentValue == FishingActorState.Idle;
        public Vector2 RotationDelta { get; private set; }
        public float ReelingInput { get; private set; }
        public float CastingIntensity { get; private set; }

        protected void Awake()
        {
            var map = InputSystem.actions.FindActionMap("Fishing");
            if (map != null)
            {
                map.Enable();
                touchAction = map.FindAction("Touch");
                touchPosition = map.FindAction("Touch Position");
                touchDelta = map.FindAction("Touch Delta");
            }

            playerModel = FishingPlayerManager.Instance.LocalPlayer.Model;
            if (CameraController.Instance.TryGetCinemachineCamera("Fishing", out var camera))
            {
                cameraPanTilt = camera.GetComponent<CinemachinePanTilt>();
                cameraStartTilt = cameraPanTilt.TiltAxis.Value;
            }
        }

        void Update()
        {
            if (touchAction != null)
            {
                if (touchAction.WasPressedThisFrame())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    if (position.x > Screen.width * 0.5f && position.y > 0 && position.x < Screen.width && position.y < Screen.height)
                        isPressed = true;
                }

                if (touchAction.WasReleasedThisFrame())
                {
                    isPressed = false;
                    RotationDelta = Vector2.zero;
                }

                if (isPressed)
                {
                    RotationDelta = touchDelta.ReadValue<Vector2>();

                    playerModel.characterPan += RotationDelta.x * sensitivity;
                    playerModel.characterPan = Mathf.Repeat(playerModel.characterPan, 360f);

                    rawTilt -= RotationDelta.y * sensitivity;
                    rawTilt = Mathf.Clamp(rawTilt, tiltLimit.x, tiltLimit.y);
                }

                Vector2 targetTiltLimit = IsPitchingMode ? pitchingTiltLimit : tiltLimit;
                currentTiltLimit = Vector2.Lerp(currentTiltLimit, targetTiltLimit, 5f * Time.deltaTime);
                playerModel.characterTilt = Mathf.Clamp(rawTilt, currentTiltLimit.x, currentTiltLimit.y);

                if (cameraPanTilt != null)
                {
                    // 캐릭터 제한을 넘어서는 초과 틸트량 산출
                    float extraTilt = rawTilt - playerModel.characterTilt;
                    cameraPanTilt.TiltAxis.Value = cameraStartTilt + extraTilt;
                }
            }
        }
    }
}