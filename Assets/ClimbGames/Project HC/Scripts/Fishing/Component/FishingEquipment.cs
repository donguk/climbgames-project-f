using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingEquipment : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public ConfigurableJoint Joint { get; protected set; }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Joint = GetComponent<ConfigurableJoint>();
        }

        //protected void ResetRotation()
        //{
        //    var connectedBody = Joint.connectedBody;
        //    if (connectedBody != null)
        //    {
        //        Joint.connectedBody = null;
        //        transform.rotation = connectedBody.transform.rotation;
        //        Joint.connectedBody = connectedBody;
        //    }
        //}

        public virtual void Attach(FishingLinePoint point)
        {
            Joint.connectedBody = null;
            // 초기 회전 리셋
            transform.rotation = point.Body.transform.rotation;
            Joint.connectedBody = point.Body;
        }
    }
}