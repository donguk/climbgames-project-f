using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ClimbGames.ProjectF
{
    // 줄에 달린 장비 (찌, 봉돌, 바늘): 자기 줄 점(Segment)의 속성을 정하고, 줄 계산 중에 그 점을 다룰 수 있음
    public class FishingLineObject : FishingEquipment, ISegmentController
    {
        [Header("Body")]
        [SerializeField] protected float mass = 0.001f;                         // 질량 (kg)
        // 몸체 지름 (m): 저항 단면적을 이 값으로 구함 (구로 봄, 부력 부피는 질량 / bodyDensity)
        // 저항 힘 = 0.5 · 매질 밀도 · Cd · 단면적 · v², 감속은 이 힘 / 질량 → 같은 크기면 무거울수록 덜 감속 (빨리 가라앉고 덜 흔들림)
        [SerializeField, Min(0f)] protected float bodyDiameter = 0.005f;
        [SerializeField, Min(0f)] protected float bodyDragScale = 1f;           // 구 대비 저항 배율 (구 1, 막대·바늘처럼 옆으로 움직이는 원기둥 약 2)
        // 몸체 재질 밀도 (kg/m³): 부력 부피 = 질량 / 이 값 → 무게의 (물 밀도 / 이 값) 만큼 부력 (강철 7850 → 13%, 납 11340 → 9%, 물보다 작으면 뜸, 찌는 buoyancyLoad 로 부피를 정함)
        [SerializeField, Min(1f)] protected float bodyDensity = 7850f;
        [SerializeField, Range(0f, 1f)] protected float groundFriction = 0.2f;  // 땅 마찰

        private Rigidbody body;

        public Rigidbody Body
        {
            get
            {
                if (body == null)
                    body = gameObject.GetOrAddComponent<Rigidbody>();
                return body;
            }
        }
        public ConfigurableJoint RuntimeJoint { get; private set; }
        public FishingLinePoint LinePoint { get; protected set; }

        // 줄 점에 등록하고 조인트로 매달림
        public virtual void AttachToLine(FishingLinePoint linePoint)
        {
            LinePoint = linePoint;
            if (linePoint.Segment != null)
                linePoint.Segment.controller = this;

            Body.isKinematic = false;

            var joint = transform.GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = transform.AddComponent<ConfigurableJoint>();
                joint.autoConfigureConnectedAnchor = false;
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
            }
            else
            {
                joint.connectedBody = null;
            }

            // 점의 자세로 맞춘 뒤 연결 (조인트는 연결 순간의 상대 회전을 기준으로 삼으므로 회전 차이 0 에서 시작)
            Vector3 position = linePoint.transform.position;
            Quaternion rotation = linePoint.transform.rotation;
            transform.SetPositionAndRotation(position, rotation);
            Body.position = position;
            Body.rotation = rotation;
            joint.connectedBody = linePoint.Body;

            RuntimeJoint = joint;
            OnAttachToLine(linePoint, joint);
        }

        // 조인트를 떼고 줄에서 분리 (줄 점 추적은 FishingLine 이 정함)
        public virtual void DetachFromLine()
        {
            if (TryGetComponent<ConfigurableJoint>(out var joint))
                Destroy(joint);
            RuntimeJoint = null;

            OnDetachFromLine(LinePoint);
        }

        protected virtual void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
        }

        protected virtual void OnDetachFromLine(FishingLinePoint point)
        {
        }

        // 물속에서 부력을 받는 몸체 부피 (m³) = 질량 / 재질 밀도 (바늘처럼 가는 모양은 지름으로 구한 부피와 크게 다름)
        protected virtual float BodyVolume => mass / bodyDensity;

        // 몸체 잠김 비율 기준: 높이와 점 위치 (기본은 지름 bodyDiameter 인 구의 중심)
        protected virtual float BodyHeight => bodyDiameter;
        protected virtual float BodyPivot => 0.5f;

        // 줄 스텝 시작: 점이 맡은 줄 질량에 장비 질량을 더하고, 몸체 크기와 땅 마찰을 넘김
        // - 공기·물 저항과 부력은 줄 몫(줄 굵기)과 몸체 몫(bodyDiameter)의 힘을 더해 전체 질량으로 나눔 (FishingLineBody.GetDragRate, Integrate)
        // - 속도 비례 감쇠(damping)는 줄 값을 그대로 씀 (진자는 무거운 장비가 끌고 가므로 장비에도 걸려야 멈춤)
        public virtual void OnPreStep(FishingLineBody.Segment segment)
        {
            float total = segment.mass + mass;
            if (total <= 0f)
                return;

            segment.bodyDiameter = bodyDiameter;
            segment.bodyDragScale = bodyDragScale;
            segment.bodyVolume = BodyVolume;
            segment.bodyHeight = BodyHeight;
            segment.bodyPivot = BodyPivot;
            segment.groundFriction = groundFriction;
            segment.mass = total;
        }

        // 서브스텝마다 적분 직후: 필요하면 자기 점을 움직임 (찌의 부력 등)
        public virtual void OnSubStep(FishingLineBody.Segment segment, float deltaTime)
        {
        }
    }
}