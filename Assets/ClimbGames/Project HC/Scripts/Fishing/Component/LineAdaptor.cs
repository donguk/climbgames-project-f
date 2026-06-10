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
    }
}