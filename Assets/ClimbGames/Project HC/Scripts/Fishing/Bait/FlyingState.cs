using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingBaitStateController
    {
        public struct FlyingStateParam : IStateParam
        {
            public Vector3 position;
            public float angle;
        }

        public class FlyingState : FishingBaitStateBase<FlyingStateParam>
        {
            public FlyingState(FishingBait bait, FishingBaitStateController controller) : base(bait, controller)
            {
            }

            protected override void OnEnter(FlyingStateParam param = default)
            {
                Bait.Body.linearVelocity = CalculateVelocity(param.position, param.angle);
                Bait.Body.excludeLayers = 0;
                Bait.Body.linearDamping = 0f;

                Bait.Joint.connectedBody = null;
                Bait.Joint.xMotion = ConfigurableJointMotion.Free;
                Bait.Joint.yMotion = ConfigurableJointMotion.Free;
                Bait.Joint.zMotion = ConfigurableJointMotion.Free;
            }

            public Vector3 CalculateVelocity(Vector3 target, float angle)
            {
                Vector3 direction = target - Bait.transform.position;
                float h = direction.y; // 높이 차 (yOffset)
                direction.y = 0; // 수평 거리 계산을 위해 y축 제거
                float d = direction.magnitude; // 수평 거리 (distance)
                float a = angle * Mathf.Deg2Rad; // 라디안 변환
                float g = Physics.gravity.magnitude;

                // 포물선 속도 공식 (분모 0 체크 포함)
                float cosA = Mathf.Cos(a);
                float v2 = (g * d * d) / (2 * cosA * cosA * (d * Mathf.Tan(a) - h));

                if (v2 <= 0)
                {
                    Debug.LogWarning("해당 각도로는 목표 지점에 도달할 수 없습니다.");
                    return Vector3.zero;
                }

                float v = Mathf.Sqrt(v2);

                // 발사 벡터 생성
                // 1. 수평 방향으로 정규화된 벡터
                Vector3 launchVec = direction.normalized;

                // 2. 수평 벡터를 위쪽(Y)으로 '각도'만큼 회전
                // 수평 성분은 cos(a), 수직 성분은 sin(a)의 비율을 가집니다.
                Vector3 finalVelocity = launchVec * cosA;
                finalVelocity.y = Mathf.Sin(a);

                // 3. 최종 속도 크기 곱하기
                return finalVelocity * v;
            }
        }
    }
}