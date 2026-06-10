using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class LineSocket : MonoBehaviour
    {
        [SerializeField] private Color gizmoColor = Color.green;

        public Rigidbody Body { get; private set; }

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
        }

        void OnDrawGizmos()
        {
            Color origin = Gizmos.color;
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.04f);
            Gizmos.color = origin;
        }
    }
}