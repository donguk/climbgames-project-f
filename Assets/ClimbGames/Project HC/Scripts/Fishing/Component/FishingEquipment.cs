using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingEquipment : MonoBehaviour
    {
        private Rigidbody body;
        private ConfigurableJoint joint;

        public Rigidbody Body => body;
        public ConfigurableJoint Joint => joint;
        public FishingLinePoint AttachPoint { get; private set; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody>();
            joint = GetComponent<ConfigurableJoint>();
        }

        public virtual void AttachToLine(FishingLinePoint point)
        {
            body.isKinematic = false;

            if (joint == null)
            {
                joint = gameObject.GetOrAddComponent<ConfigurableJoint>();
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
            transform.rotation = point.transform.rotation;
            joint.connectedBody = point.Body;

            AttachPoint = point;
            AttachPoint.SetTrackingTarget(null);
        }

        public virtual void CastingThrow(Vector3 velocity)
        {
            Destroy(joint);

            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.linearDamping = 0f;

            AttachPoint.SetTrackingTarget(transform);
        }
    }
}