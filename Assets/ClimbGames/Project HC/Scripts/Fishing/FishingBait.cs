using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum BaitState
    {
        Idle,
        Flying,
        Floating,
    }

    public class FishingBait : MonoBehaviour
    {
        [SerializeField] private Rigidbody baitBody;
        [SerializeField] private ConfigurableJoint configurableJoint;

        public BaitState State { get; private set; }

        void Awake()
        {
            if (baitBody == null)
                baitBody = GetComponent<Rigidbody>();

            if (configurableJoint == null)
                configurableJoint = GetComponent<ConfigurableJoint>();
        }

        public void SetHook(Transform hook)
        {
            transform.position = hook.position;

            baitBody.linearVelocity = Vector3.zero;
            baitBody.angularVelocity = Vector3.zero;

            configurableJoint.connectedBody = hook.GetComponent<Rigidbody>();
            configurableJoint.xMotion = ConfigurableJointMotion.Limited;
            configurableJoint.yMotion = ConfigurableJointMotion.Limited;
            configurableJoint.zMotion = ConfigurableJointMotion.Limited;

            State = BaitState.Idle;
        }

        public void Throw(Vector3 target, float angle)
        {
            baitBody.isKinematic = false;
            configurableJoint.connectedBody = null;
            configurableJoint.xMotion = ConfigurableJointMotion.Free;
            configurableJoint.yMotion = ConfigurableJointMotion.Free;
            configurableJoint.zMotion = ConfigurableJointMotion.Free;


            //var temp = AssetManager.Instantiate<Rigidbody>("FishingBait_temp");
            //temp.transform.position = transform.position;

            Vector3 direction = target - transform.position;
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
                return;
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
            baitBody.linearVelocity = finalVelocity * v;
            baitBody.angularVelocity = new Vector3(Random.Range(-5f, 5f), 0, 10f);

            State = BaitState.Flying;
        }
    }
}