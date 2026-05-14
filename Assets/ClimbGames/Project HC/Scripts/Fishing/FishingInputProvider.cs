using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ClimbGames.Project_HC
{
    public abstract class CastingInput
    {
        public float intensity;
        public Vector2 delta;
        public bool isPressed;

        protected Action onDragStart;
        protected Action onDragEnd;

        public event Action dragStart
        {
            add { onDragStart += value; }
            remove { onDragStart -= value; }
        }
        public event Action dragEnd
        {
            add { onDragEnd += value; }
            remove { onDragEnd -= value; }
        }
    }

    public class FishingInputProvider : MonoBehaviour
    {
        class CastingAction : CastingInput
        {
            public Vector2 pressedPosition;
            public Vector2 dargDelta;
            public bool isDrag;

            public void OnDragStart() => onDragStart?.Invoke();

            public void OnDrag(Vector2 position, Vector2 delta)
            {
                intensity = Mathf.Clamp((pressedPosition.y - position.y) / (Screen.height * 0.25f), 0f, 1f);
                this.delta = delta;
            }

            public void OnDragEnd() => onDragEnd?.Invoke();
        }

        private InputAction touchAction;
        private InputAction touchPosition;
        private InputAction touchDelta;

        private CastingAction castingAction = new CastingAction();
        public CastingInput CastingInput => castingAction;
        public float ReelingValue { get; private set; }

        void Awake()
        {
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
            if (Mouse.current != null)
            {
                ReelingValue = Mouse.current.rightButton.isPressed ? 1f : 0f;
            }

            if (touchAction != null)
            {
                if (touchAction.WasPressedThisFrame())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    if (position.x > 0 && position.y > 0 && position.x < Screen.width && position.y < Screen.height)
                    {
                        castingAction.isPressed = true;
                        castingAction.pressedPosition = position;
                    }
                }

                if (touchAction.IsPressed())
                {
                    var position = touchPosition.ReadValue<Vector2>();
                    if (castingAction.isDrag)
                    {
                        castingAction.OnDrag(position, touchDelta.ReadValue<Vector2>());
                    }
                    else
                    {
                        if (castingAction.isPressed)
                        {
                            float dist = Vector3.Distance(castingAction.pressedPosition, position);
                            if (dist > 4f)
                            {
                                castingAction.isDrag = true;
                                castingAction.OnDragStart();
                            }
                        }
                    }
                }

                if (touchAction.WasReleasedThisFrame())
                {
                    if (castingAction.isDrag)
                        castingAction.OnDragEnd();

                    castingAction.isDrag = false;
                    castingAction.isPressed = false;
                    castingAction.delta = Vector2.zero;
                }
            }
        }
    }
}