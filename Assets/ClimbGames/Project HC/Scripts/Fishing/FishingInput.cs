using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class FishingInput : MonoSingleton<FishingInput>
    {
        private InputAction touchAction;
        private InputAction touchPosition;
        private InputAction touchDelta;

        public Vector2 PressedPosition { get; private set; }
        public bool IsPressed { get; private set; }
        public Vector2 DragDelta { get; private set; }
        public float Reeling { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            var map = InputSystem.actions.FindActionMap("Fishing");
            if (map != null)
            {
                map.Enable();
                touchAction = map.FindAction("Touch");
                touchPosition = map.FindAction("Touch Position");
                touchDelta = map.FindAction("Touch Delta");
            }
        }

        void Update()
        {
            if (touchAction != null)
            {
                if (touchAction.WasPressedThisFrame())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    if (position.x > 0 && position.y > 0 && position.x < Screen.width && position.y < Screen.height)
                    {
                        IsPressed = true;
                        PressedPosition = position;
                    }
                }

                if (touchAction.IsPressed())
                {
                    DragDelta = touchDelta.ReadValue<Vector2>();
                }

                if (touchAction.WasReleasedThisFrame())
                {
                    IsPressed = false;
                    DragDelta = Vector2.zero;
                }
            }
        }

        public void SetReeling(bool value)
        {
            Reeling = value ? 1f : 0f;
        }
    }
}