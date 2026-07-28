using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class WorldCamera : MonoBehaviour
    {
        [SerializeField] private WorldBounds worldBounds;
        [SerializeField] private float dragSpeed = 1.0f;
        [SerializeField] private float zoomSpeed = 4.0f;

        private InputAction touchAction;
        private InputAction touchDelta;
        private InputAction zoomAction;

        protected void Awake()
        {
            var map = InputSystem.actions.FindActionMap("World");
            if (map != null)
            {
                map.Enable();
                touchAction = map.FindAction("Touch");
                touchDelta = map.FindAction("Touch Delta");
                zoomAction = map.FindAction("Zoom");
            }
        }

        private void Update()
        {
            HandleZoom();
            HandleDrag();
        }

        private void LateUpdate()
        {
            // 이동/줌 완료 후 DynamicBound에서 갱신된 Box Collider 크기를 기준으로 카메라 위치를 가둡니다.
            ClampCameraPosition();
        }

        private void HandleZoom()
        {
            if (worldBounds == null) return;

            // BoxCollider의 월드 좌표 기준 Min/Max 계산
            Bounds bounds = worldBounds.CameraBounds;

            Vector2 scroll = zoomAction.ReadValue<Vector2>();
            float delta = scroll.y;
            if (Mathf.Abs(delta) > 0.01f)
            {
                Vector3 pos = transform.position;
                pos.y -= delta * zoomSpeed;
                pos.y = Mathf.Clamp(pos.y, bounds.min.y, bounds.max.y);
                transform.position = pos;
            }
        }

        private void HandleDrag()
        {
            if (touchAction.IsPressed())
            {
                Vector3 delta = touchDelta.ReadValue<Vector2>();

                // 드래그 방향에 맞춰 X, Z 이동 (높이 Y에 비례해 감도 조정)
                Vector3 move = new Vector3(-delta.x, 0, -delta.y) * (dragSpeed * (transform.position.y / 10f));
                transform.position += move;
            }
        }

        private void ClampCameraPosition()
        {
            if (worldBounds == null) return;

            // BoxCollider의 월드 좌표 기준 Min/Max 계산
            Bounds bounds = worldBounds.CameraBounds;

            Vector3 pos = transform.position;

            // BoxCollider의 center/size에 맞춰 X, Z 위치 가두기
            pos.x = Mathf.Clamp(pos.x, bounds.min.x, bounds.max.x);
            pos.z = Mathf.Clamp(pos.z, bounds.min.z, bounds.max.z);

            transform.position = pos;
        }
    }
}