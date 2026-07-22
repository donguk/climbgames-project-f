using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLineObject : MonoBehaviour
    {
        private Rigidbody body;
        private ConfigurableJoint joint;

        public Rigidbody Body => body;
        public ConfigurableJoint Joint => joint;
        public FishingLinePoint ConnectedPoint { get; private set; }

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

            ConnectedPoint = point;
            ConnectedPoint.SetTrackingTarget(null);
        }

        public virtual void DetachFromLine()
        {
            if (Joint != null)
                Destroy(Joint);

            if (ConnectedPoint != null)
                ConnectedPoint.SetTrackingTarget(transform);
        }
    }
}