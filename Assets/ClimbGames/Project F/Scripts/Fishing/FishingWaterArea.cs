using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingWaterArea : MonoBehaviour
    {
        [SerializeField] private float radius = 1f;

        public Vector3 Center => transform.position;
        public float Radius => radius;

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
            };
        }

        public WaterSegmentInfo GetInnerSegmentInfo(Transform target)
        {
            Vector3 targetPos = target.transform.position;
            Vector3 waterCenter = Center;

            targetPos.y = waterCenter.y;
            Vector3 centerDir = waterCenter - targetPos;

            Vector3 forward = target.transform.forward;
            forward.y = 0f;

            // 조작 방향 선상으로의 투영 거리
            float projDist = Vector3.Dot(centerDir, forward);
            if (projDist < 0f)
                return WaterSegmentInfo.Default;

            // 조작 선과 원 중심 사이의 수직 거리
            Vector3 projPoint = targetPos + forward * projDist;
            float perpendicularDist = Vector3.Distance(waterCenter, projPoint);

            float safeRadius = radius * 0.9f;
            if (perpendicularDist < safeRadius)
            {
                float halfCord = Mathf.Sqrt((safeRadius * safeRadius) - (perpendicularDist * perpendicularDist));

                float minDist = projDist - halfCord;
                float maxDist = projDist + halfCord;

                return new WaterSegmentInfo()
                {
                    startPos = targetPos + forward * minDist,
                    endPos = targetPos + forward * maxDist,
                    length = halfCord * 2f,
                };
            }

            return WaterSegmentInfo.Default;
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