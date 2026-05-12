using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum BaitState
    {
        Hanging,
        Casting,
        Located,
    }

    public class FishingBait : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private ConfigurableJoint configurableJoint;

        private int locationFlag;

        public BaitState State { get; private set; }
        public int LocationFlag => locationFlag;
        public Rigidbody Body => body;
        public Vector3 Velocity => body.linearVelocity;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (configurableJoint == null)
                configurableJoint = GetComponent<ConfigurableJoint>();
        }

        void Update()
        {
            if ((locationFlag & 1 << FishingLayer.Water) != 0)
                body.linearDamping = Mathf.MoveTowards(body.linearDamping, 1f, Time.deltaTime * 5f);
        }

        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=green>OnCollisionEnter</color>: {collision.gameObject.name}");
            locationFlag |= 1 << collision.gameObject.layer;

            SetState(BaitState.Located);
        }

        void OnCollisionExit(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=red>OnCollisionExit</color>: {collision.gameObject.name}");
            locationFlag &= ~(1 << collision.gameObject.layer);

            if ((locationFlag & 1 << FishingLayer.Ground) != 0)
                body.linearDamping = 5f;
        }

        public void SetState(BaitState state)
        {
            State = state;

            switch (state)
            {
                case BaitState.Hanging:
                    {
                        body.linearVelocity = Vector3.zero;
                        body.angularVelocity = Vector3.zero;
                        body.linearDamping = 1f;
                        body.excludeLayers |= 1 << FishingLayer.Water | 1 << FishingLayer.Ground;

                        configurableJoint.xMotion = ConfigurableJointMotion.Limited;
                        configurableJoint.yMotion = ConfigurableJointMotion.Limited;
                        configurableJoint.zMotion = ConfigurableJointMotion.Limited;
                        break;
                    }
                case BaitState.Casting:
                    {
                        body.excludeLayers = 0;
                        body.linearDamping = 0f;

                        configurableJoint.connectedBody = null;
                        configurableJoint.xMotion = ConfigurableJointMotion.Free;
                        configurableJoint.yMotion = ConfigurableJointMotion.Free;
                        configurableJoint.zMotion = ConfigurableJointMotion.Free;
                        break;
                    }
                case BaitState.Located:
                    {
                        // Y축 속도는 95% 제거, 수평 속도는 70% 제거
                        Vector3 velocity = body.linearVelocity;
                        body.linearVelocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.05f, velocity.z * 0.5f);
                        body.angularVelocity *= 0.1f;
                        body.linearDamping = 5f;
                        break;
                    }
            }
        }

        public void Connect(Transform lineEnd)
        {
            transform.position = lineEnd.position;
            configurableJoint.connectedBody = lineEnd.GetComponent<Rigidbody>();
            locationFlag = 0;
            body.linearDamping = 1f;

            SetState(BaitState.Hanging);
        }

        public void Cast(Vector3 target, float angle)
        {
            SetState(BaitState.Casting);

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
            body.linearVelocity = finalVelocity * v;
            //baitBody.angularVelocity = new Vector3(Random.Range(-5f, 5f), 0, 10f);

            State = BaitState.Casting;
        }

        public void Drag(Vector3 lineDir, float tensionIntensity)
        {
            if (tensionIntensity <= 0) return;

            if ((locationFlag & 1 << FishingLayer.Ground) != 0 && Physics.Raycast(transform.position, Vector3.down, out var hit, 0.5f, 1 << FishingLayer.Ground))
            {
                Vector3 surfaceDir = Vector3.ProjectOnPlane(lineDir, hit.normal).normalized;

                float verticalScale = Mathf.Abs(lineDir.y);
                Vector3 finalDir = Vector3.Slerp(surfaceDir, lineDir, verticalScale).normalized;

                float force = 100f * tensionIntensity;
                body.AddForce(finalDir * force, ForceMode.Acceleration);
            }
            else if ((locationFlag & 1 << FishingLayer.Water) != 0)
            {
                // 힘 적용 (ForceMode.Acceleration은 질량 무시하고 가속)
                float force = 100f * tensionIntensity;
                body.AddForce(lineDir * force, ForceMode.Acceleration);
            }
        }
    }
}