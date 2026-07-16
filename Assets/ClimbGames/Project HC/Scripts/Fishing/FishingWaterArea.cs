using UnityEngine;

namespace ClimbGames.Project_HC
{
    public struct FishStruggleArea
    {
        public Vector3 center;
        public float range;
    }

    public class FishingWaterArea : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;
        [SerializeField] private Transform strugglePoint;
        [SerializeField] private float struggleRange = 1f;

        public Vector3 Center => transform.position;

        public FishStruggleArea GetStruggleArea(Vector3 position, float range)
        {
            strugglePoint.position = position;
            struggleRange = range;

            return CalculateStruggleArea(position, range);
        }

        FishStruggleArea CalculateStruggleArea(Vector3 position, float range)
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

            return new FishStruggleArea()
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

            //
            if (strugglePoint != null)
            {
                var struggleArea = GetStruggleArea(strugglePoint.position, struggleRange);
                UnityEditor.Handles.color = Color.green;
                UnityEditor.Handles.DrawWireDisc(struggleArea.center, Vector3.up, struggleArea.range);
            }
        }
#endif
    }
}