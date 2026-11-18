using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace ClimbGames.ProjectF
{
    // 줄에 달린 장비 (찌, 봉돌, 바늘): 자기 줄 점(Segment)의 속성을 정하고, 줄 계산 중에 그 점을 다룰 수 있음
    public class FishingLineObject : FishingEquipment, ILineAttachable, ISegmentController
    {
        [Header("Line Segment")]
        [SerializeField] protected float mass = 0.001f;                         // 질량 (kg)
        [SerializeField] protected float drag = 0.03f;                          // 공기 저항 (1/m), 0.5 · 공기 밀도 · 항력 계수 · 면적 / 질량
        [FormerlySerializedAs("pullDrag")]
        [SerializeField] protected float waterDrag = 3f;                        // 물 저항 (1/s)
        [SerializeField, Range(0f, 1f)] protected float groundFriction = 0.2f;  // 땅 마찰

        private Rigidbody body;

        public Transform Target => transform;
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
        public FishingLinePoint LinePoint { get; private set; }

        // 줄 점에 장비로 등록만 함 (조인트 없음, 봉돌처럼 줄 점 속성만 정하는 장비)
        public void BindToLine(FishingLinePoint linePoint)
        {
            LinePoint = linePoint;
            if (linePoint.Segment != null)
                linePoint.Segment.controller = this;
        }

        // 줄 점에 등록하고 조인트로 매달림
        public virtual void AttachToLine(FishingLinePoint linePoint)
        {
            BindToLine(linePoint);
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

            // 초기 회전 리셋
            transform.rotation = transform.rotation;
            joint.connectedBody = linePoint.Body;

            RuntimeJoint = joint;
        }



        void ILineAttachable.OnAttached(FishingLinePoint point, ConfigurableJoint joint)
        {
            // 연결 포인트 저장
            LinePoint = point;
            RuntimeJoint = joint;

            // 라인에 오브젝트 연결
            if (TracksLinePoint)
                LinePoint.SetTrackingTarget(null);

            OnAttachToLine(point, joint);
        }

        void ILineAttachable.OnDetached(FishingLinePoint point)
        {
            // 연결되었던 포인트 저장
            LinePoint = point;
            RuntimeJoint = null;

            // 라인의 포인트가 오브젝트를 추적
            if (TracksLinePoint)
                LinePoint.SetTrackingTarget(transform);

            OnDetachFromLine(point);
        }

        // 떨어졌을 때 줄의 점이 이 오브젝트를 고정점으로 추적하는지 (찌는 줄의 찌 점을 따라가므로 추적하지 않음)
        protected virtual bool TracksLinePoint => true;

        protected virtual void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
        }

        protected virtual void OnDetachFromLine(FishingLinePoint point)
        {
        }

        // 줄 스텝 시작: 점이 맡은 줄 질량·저항 위에 장비 값을 더함 (mass 0 이면 줄만 있는 것과 같음)
        // 공기 저항(drag)은 장비 몫으로 따로 계산되므로 그대로, 속도 비례 감쇠(damping)는 줄 값을 그대로 씀 (진자는 무거운 장비가 끌고 가므로 장비에도 걸려야 멈춤)
        // 물 저항과 땅 마찰은 질량 비율로 섞음
        public virtual void OnPreStep(FishingLineBody.Segment segment)
        {
            float total = segment.mass + mass;
            if (total <= 0f)
                return;

            segment.drag = drag;
            segment.waterDrag = (segment.mass * segment.waterDrag + mass * waterDrag) / total;
            segment.groundFriction = (segment.mass * segment.groundFriction + mass * groundFriction) / total;
            segment.mass = total;
        }

        // 서브스텝마다 적분 직후: 필요하면 자기 점을 움직임 (찌의 부력 등)
        public virtual void OnSubStep(FishingLineBody.Segment segment, float deltaTime)
        {
        }
    }
}