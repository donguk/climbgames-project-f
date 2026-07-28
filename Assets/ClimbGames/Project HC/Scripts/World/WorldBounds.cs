using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ClimbGames.Project_HC
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(BoxCollider))]
    public class WorldBounds : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Vector3 fullSize = new Vector3(100f, 50f, 100f);

        private BoxCollider boxCollider;

        public Bounds CameraBounds => boxCollider.bounds;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider>();
            if (mainCamera == null)
                mainCamera = Framework.MainCamera;
        }

        private void LateUpdate()
        {
            if (mainCamera == null || boxCollider == null) return;

            UpdateColliderBounds();
        }

        private void UpdateColliderBounds()
        {
            Vector3 position = transform.position;

            // 1. 카메라와 지면(또는 Target) 사이의 수직/대각선 거리 및 각도 계산
            // 카메라의 높이(Y)를 기준으로 화면 절반 높이(Half Height) 계산
            float cameraHeight = Mathf.Abs(mainCamera.transform.position.y - position.y);

            // 2. 현재 FOV 수치로 화면 절반 폭/높이 (World Units) 계산
            float fovRad = mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfHeight = cameraHeight * Mathf.Tan(fovRad);
            float halfWidth = halfHeight * mainCamera.aspect;

            // 3. FOV에 따른 Padding(안쪽 여백)을 적용하여 Box Collider Size 계산
            // FOV가 좁아질수록 halfWidth, halfHeight가 줄어들어서 Box Size가 늘어납니다 (더 멀리 이동 가능).
            float newSizeX = Mathf.Max(1f, fullSize.x - (halfWidth * 2f));
            float newSizeZ = Mathf.Max(1f, fullSize.z - (halfHeight * 2f));

            // Size 설정 (Y는 fullSize.y 이하로 제어)
            Vector3 size = boxCollider.size;
            size.x = newSizeX;
            size.z = newSizeZ;
            size.y = Mathf.Min(boxCollider.size.y, fullSize.y);

            // 4. Center.y 로컬 범위 계산 (Local Space 기준)
            // fullSize 영역 중심이 (0, 0, 0)일 때 size.y를 가진 박스가 움직일 수 있는 로컬 Y 한계값

            float maxLocalCenterX = (fullSize.x - size.x) * 0.5f;
            float maxLocalCenterY = (fullSize.y - size.y) * 0.5f;
            float maxLocalCenterZ = (fullSize.z - size.z) * 0.5f;

            // 현재 설정되어 있는 center.y를 로컬 범주 안으로 Clamp
            Vector3 center = boxCollider.center;
            center.x = Mathf.Clamp(center.x, -maxLocalCenterX, maxLocalCenterX);
            center.y = Mathf.Clamp(center.y, -maxLocalCenterY, maxLocalCenterY);
            center.z = Mathf.Clamp(center.z, -maxLocalCenterZ, maxLocalCenterZ);

            // 5. 최종 적용
            boxCollider.size = size;
            boxCollider.center = center;
        }

        // Scene 뷰에서 전체 맵 기즈모 확인용
        private void OnDrawGizmos()
        {
            if (boxCollider != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position, fullSize);

#if UNITY_EDITOR
                if (Selection.activeGameObject != gameObject)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position + boxCollider.center, boxCollider.size);
                }
#endif
            }
        }
    }
}