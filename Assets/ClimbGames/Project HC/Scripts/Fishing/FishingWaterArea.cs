using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingWaterArea : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;

        public Vector3 Center => transform.position;

        public WaterAreaInfo GetInnerAreaInfo(Vector3 position, float range)
        {
            // 전체 영역 정보
            Vector3 waterCenter = transform.position;
            float waterRange = radius;
            position.y = waterCenter.y;

            // 원 사이즈 제한
            float validRange = Mathf.Min(waterRange, range);
            float maxCenterRadius = waterRange - validRange;

            Vector3 offsetFromCenter = position - waterCenter;
            Vector3 clampedOffset = Vector3.ClampMagnitude(offsetFromCenter, maxCenterRadius);
            Vector3 validCenter = waterCenter + clampedOffset;

            return new WaterAreaInfo()
            {
                center = validCenter,
                range = validRange,
            }; ;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // 전체 영역
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, radius);
        }
#endif
    }
}