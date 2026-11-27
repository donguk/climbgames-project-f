using ClimbGames.UI;
using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public class FishingSpot : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private FishingWaterArea[] waterAreas;

        private UIFishingSpot uiSpot;

        public Transform SpawnPoint => spawnPoint;
        public FishingWaterArea[] WaterAreas => waterAreas;
        public FishingWaterArea WaterArea => waterAreas[0];

        void Awake()
        {
            uiSpot = UIManager.Instance.CreateUI<UIFishingSpot>("UIFisingSpot", UILayer.World);
            uiSpot.Setup(this);

            WorldManager.Instance.CurrentMode.Subscribe(_ => uiSpot.gameObject.SetActive(_ == WorldMode.Map)).AddTo(this);
        }

        void OnDestroy()
        {
            if (uiSpot != null)
                UIManager.HideSafely(uiSpot);
        }

        public WaterSegmentInfo GetCastingArea(Transform target)
        {
            WaterSegmentInfo targetInfo = WaterSegmentInfo.Default;
            Vector3 targetPos = target.transform.position;

            float highestScore = 0f;
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                var areaInfo = waterAreas[i].GetInnerSegmentInfo(target);
                targetPos.y = waterAreas[i].Center.y;

                // 범위가 길고, 가까운 영역 선택을 위한 스코어 계산
                float score = areaInfo.length / Vector3.Distance(targetPos, waterAreas[i].Center);
                if (highestScore < score)
                {
                    targetInfo = areaInfo;
                    highestScore = score;
                }
            }

            return targetInfo;
        }

        // 수역 = waterAreas 원들의 합집합 (+ 헤엄칠 만큼 깊은 곳)

        private const float CastHeight = 2f;
        private const float FreeDistanceStep = 0.25f;
        private const int MaxFreeDistanceSteps = 64;
        private const float SteerAngleStep = 15f;
        private const int MaxSteerTries = 12;

        // 어느 한 원 안이라도 들어 있는지 (수평)
        public bool IsInsideAreas(Vector3 position, float margin = 0f)
        {
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                if (waterAreas[i].Contains(position, margin))
                    return true;
            }
            return false;
        }

        // 원 밖이면 가장 가까운 원의 가장자리로 (수평, 높이는 그대로)
        public Vector3 ClampInAreas(Vector3 position)
        {
            if (IsInsideAreas(position))
                return position;

            Vector3 closest = position;
            float shortestSqr = float.MaxValue;
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                Vector3 point = waterAreas[i].ClosestPoint(position);
                float sqr = (point - position).sqrMagnitude;
                if (sqr < shortestSqr)
                {
                    closest = point;
                    shortestSqr = sqr;
                }
            }
            return closest;
        }

        // position 을 담은(없으면 가장 가까운) 영역의 수면 높이
        public float GetSurfaceHeight(Vector3 position)
        {
            FishingWaterArea nearest = null;
            float shortestDist = float.MaxValue;
            for (int i = 0; i < waterAreas.Length; ++i)
            {
                if (waterAreas[i].Contains(position))
                    return waterAreas[i].Center.y;

                Vector3 offset = waterAreas[i].Center - position;
                offset.y = 0f;
                float dist = offset.magnitude - waterAreas[i].Radius;
                if (dist < shortestDist)
                {
                    nearest = waterAreas[i];
                    shortestDist = dist;
                }
            }
            return nearest != null ? nearest.Center.y : position.y;
        }

        // 물고기가 있을 수 있는 높이 범위: topY (수면 아래) ~ floorY (바닥 위 / 최대 깊이)
        public void GetDepthRange(Vector3 position, in FishDepthSetting depth, out float topY, out float floorY)
        {
            float surfaceY = GetSurfaceHeight(position);
            topY = surfaceY - depth.surfaceMargin;
            floorY = surfaceY - depth.maxDepth;

            Vector3 origin = new Vector3(position.x, surfaceY + CastHeight, position.z);
            float castDistance = CastHeight + depth.maxDepth + 1f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, castDistance, 1 << FishingLayer.Ground, QueryTriggerInteraction.Ignore))
                floorY = Mathf.Max(floorY, hit.point.y + depth.bottomMargin);
        }

        // 원 안이고, 헤엄칠 만큼 깊은지
        public bool IsInWater(Vector3 position, in FishDepthSetting depth)
        {
            if (IsInsideAreas(position) == false)
                return false;

            GetDepthRange(position, depth, out float topY, out float floorY);
            return topY - floorY >= depth.minSwimDepth;
        }

        // dir(수평) 으로 나아갈 때 수역을 벗어나기까지 거리 (떨어진 원으로는 건너뛰지 않음)
        public float GetFreeDistance(Vector3 position, Vector3 direction, float maxDistance, in FishDepthSetting depth)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f || maxDistance <= 0f)
                return 0f;

            direction.Normalize();
            int steps = Mathf.Min(MaxFreeDistanceSteps, Mathf.CeilToInt(maxDistance / FreeDistanceStep));
            for (int i = 1; i <= steps; ++i)
            {
                float distance = Mathf.Min(maxDistance, i * FreeDistanceStep);
                if (IsInWater(position + direction * distance, depth) == false)
                    return (i - 1) * FreeDistanceStep;
            }
            return maxDistance;
        }

        // 앞(lookAhead)이 수역 밖이면, awayDir(플레이어 반대) 쪽부터 번갈아 돌려 들어갈 수 있는 방향을 찾음
        public Vector3 SteerInWater(Vector3 position, Vector3 heading, float lookAhead, Vector3 awayDir, in FishDepthSetting depth)
        {
            heading.y = 0f;
            awayDir.y = 0f;
            if (heading.sqrMagnitude < 0.0001f)
                heading = awayDir.sqrMagnitude > 0.0001f ? awayDir : Vector3.forward;
            heading.Normalize();

            if (IsInWater(position + heading * lookAhead, depth))
                return heading;

            // 플레이어에게서 먼 쪽 회전 방향 우선
            Vector3 plus = Quaternion.AngleAxis(SteerAngleStep, Vector3.up) * heading;
            Vector3 minus = Quaternion.AngleAxis(-SteerAngleStep, Vector3.up) * heading;
            float firstSign = Vector3.Dot(plus, awayDir) >= Vector3.Dot(minus, awayDir) ? 1f : -1f;

            for (int i = 1; i <= MaxSteerTries; ++i)
            {
                float sign = (i % 2 == 1) ? firstSign : -firstSign;
                float angle = SteerAngleStep * ((i + 1) / 2) * sign;
                Vector3 candidate = Quaternion.AngleAxis(angle, Vector3.up) * heading;
                if (IsInWater(position + candidate * lookAhead, depth))
                    return candidate;
            }

            // 못 찾으면: 원 밖이면 원 쪽으로, 안이면 플레이어 쪽(줄 방향)으로
            Vector3 fallback = IsInsideAreas(position) ? -awayDir : ClampInAreas(position) - position;
            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : heading;
        }
    }
}