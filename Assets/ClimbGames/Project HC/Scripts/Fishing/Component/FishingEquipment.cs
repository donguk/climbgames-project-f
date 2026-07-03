using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishingEquipmentAttachType
    {
        Joint,
        Transform,
    }

    public class FishingEquipment : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public ConfigurableJoint Joint { get; private set; }
        public FishingLinePoint Point { get; private set; }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Joint = GetComponent<ConfigurableJoint>();
        }

        public virtual void Attach(FishingLinePoint point)
        {
            Joint.connectedBody = null;
            // 초기 회전 리셋(transform.up 회전과 다름)
            transform.rotation = point.Body.transform.rotation;
            Joint.connectedBody = point.Body;

            Point = point;
        }
    }
}