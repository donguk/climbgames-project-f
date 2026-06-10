using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class LineAdaptor : MonoBehaviour
    {
        public Rigidbody Body { get; private set; }
        public ConfigurableJoint Joint { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Joint = GetComponent<ConfigurableJoint>();
        }

        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"[LineAdaptor] {gameObject.name} <color=green>OnCollisionEnter</color>: {collision.gameObject.name}");
        }

        void OnCollisionExit(Collision collision)
        {
            Debug.Log($"[LineAdaptor] {gameObject.name} <color=red>OnCollisionExit</color>: {collision.gameObject.name}");
        }

        public virtual void Attach(LineSocket socket)
        {
            transform.position = socket.transform.position;
            Joint.connectedBody = socket.Body;
        }
    }
}