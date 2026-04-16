using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class FishingInputProvider : MonoBehaviour
    {
        class InternalCastAction : FishingInput.ICastAction
        {
            public float loadIntensity { get; set; }
            public Vector2 pressedPosition { get; set; }
            public Vector2 direction { get; set; }

            public event Action onPressed;
            public event Action onReleased;
            public event Action onCanceled;

            public void OnPressed() => onPressed?.Invoke();
            public void OnReleased() => onReleased?.Invoke();
            public void OnCanceled() => onCanceled?.Invoke();
        }

        private InputActionMap actionMap;
        private InputAction touchAction;
        private InputAction touchPosition;

        private InternalCastAction castAction = new InternalCastAction();
        public FishingInput.ICastAction CastAction => castAction;

        void Awake()
        {
            actionMap = InputSystem.actions.FindActionMap("Fishing");
            if (actionMap != null)
            {
                actionMap.Enable();
                touchAction = actionMap.FindAction("Touch");
                touchPosition = actionMap.FindAction("Touch Position");
            }
        }

        void Update()
        {
            if (touchAction != null)
            {
                if (touchAction.WasPressedThisFrame())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    if (IsInScreen(position))
                    {
                        castAction.pressedPosition = position;
                        castAction.OnPressed();
                    }
                }

                if (touchAction.IsPressed())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    float diff = Mathf.Clamp(castAction.pressedPosition.y - position.y, 0f, Screen.height);
                    castAction.loadIntensity = Mathf.Clamp01(diff / (Screen.height * 0.4f));

                    // 드래그 방향 계산 (조준용)
                    //if (diff > 10f)
                    //{
                    //    castAction.direction = (castAction.pressedPosition - position).normalized;
                    //}
                }

                if (touchAction.WasReleasedThisFrame())
                {
                    //var position = touchPosition.ReadValue<Vector2>();
                    if (castAction.loadIntensity > 0f)
                    {
                        castAction.OnReleased();
                    }
                    else
                    {
                        castAction.OnCanceled();
                    }

                    // 상태 초기화
                    castAction.loadIntensity = 0f;
                    castAction.direction = Vector2.zero;
                }
            }
        }

        bool IsInScreen(Vector2 position)
        {
            return position.x > 0 && position.y > 0 && position.x < Screen.width && position.y < Screen.height;
        }
    }
}