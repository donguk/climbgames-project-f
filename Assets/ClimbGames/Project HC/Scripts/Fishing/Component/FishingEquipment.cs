using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingEquipment : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public ConfigurableJoint Joint { get; private set; }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Joint = GetComponent<ConfigurableJoint>();
        }

        public virtual void Attach(FishingLinePoint point)
        {
            transform.position = point.transform.position;
            Joint.connectedBody = point.Body;
        }
    }
}