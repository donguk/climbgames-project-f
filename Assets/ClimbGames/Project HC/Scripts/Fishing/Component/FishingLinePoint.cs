using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLinePoint : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private float height;
        [SerializeField] private Color gizmoColor = Color.green;

        protected FishingLine line;

        public Rigidbody Body => body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public virtual void Initialize(FishingLine line)
        {
            this.line = line;
        }

        public void SetHeight(float height)
        {
            this.height = height;
        }

        public virtual void UpdatePosition()
        {
            float visualLength = line.VisualLength;
            float lenght = visualLength - (visualLength * (height / line.Length));

            for (int i = 0; i < line.PositionCount - 1; ++i)
            {
                float diff = lenght - (line.SegmentLength * i);
                if ((diff >= 0f && diff <= line.SegmentLength) || (i == 0 && diff < 0f) || (i == line.PositionCount - 2))
                {
                    diff = Mathf.Clamp(diff, 0f, line.SegmentLength);

                    Vector3 position = line.GetPosition(i);
                    transform.up = (position - line.GetPosition(i + 1)).normalized;
                    transform.position = position + transform.up * diff * -1f;
                    break;
                }
            }
        }

        void OnDrawGizmos()
        {
            if (enabled)
            {
                Color origin = Gizmos.color;
                Gizmos.color = gizmoColor;
                Gizmos.DrawSphere(transform.position, 0.04f);
                Gizmos.color = origin;
            }
        }
    }
}