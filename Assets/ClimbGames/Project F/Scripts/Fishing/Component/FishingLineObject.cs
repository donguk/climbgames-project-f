using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingLineObject : FishingEquipment, ILineAttachable
    {
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

        void ILineAttachable.OnAttached(FishingLinePoint point, ConfigurableJoint joint)
        {
            // 연결 포인트 저장
            LinePoint = point;
            RuntimeJoint = joint;

            // 라인에 오브젝트 연결
            LinePoint.SetTrackingTarget(null);
            OnAttachToLine(point, joint);
        }

        void ILineAttachable.OnDetached(FishingLinePoint point)
        {
            // 연결되었던 포인트 저장
            LinePoint = point;
            RuntimeJoint = null;

            // 라인의 포인트가 오브젝트를 추적
            LinePoint.SetTrackingTarget(transform);
            OnDetachFromLine(point);
        }

        protected virtual void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
        }

        protected virtual void OnDetachFromLine(FishingLinePoint point)
        {
        }
    }
}