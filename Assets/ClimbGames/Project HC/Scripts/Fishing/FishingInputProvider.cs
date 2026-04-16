using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class FishingInputProvider : MonoBehaviour
    {
        private InputActionMap actionMap;
        private InputAction touchAction;
        private InputAction touchPosition;

        public event Action onPull;
        public event Action onCast;

        public Vector3 PressedPosition { get; private set; }

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
                    PressedPosition = touchPosition.ReadValue<Vector2>();
                    onPull?.Invoke();
                }

                if (touchAction.IsPressed())
                {
                    PressedPosition = touchPosition.ReadValue<Vector2>();
                    Debug.Log($"[FishingInputProvider] casting pressed position: {PressedPosition}");
                }

                if (touchAction.WasReleasedThisFrame())
                    onCast?.Invoke();
            }
        }
    }
}