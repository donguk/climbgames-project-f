using R3;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLinePoint : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private float height;
        [SerializeField] private Color gizmoColor = Color.green;

        private LinearTween targetTween = new LinearTween(0.5f);
        private Subject<bool> onKinematicChanged = new Subject<bool>();

        public Rigidbody Body => body;
        public float Height => height;
        public Vector3 Up { get; private set; }
        public Transform TrackingTarget { get; private set; }
        public FishingLineBody.LinePoint PinnedPoint { get; private set; }
        public bool HasTarget => TrackingTarget != null;
        public bool HasForce => body.isKinematic == false || HasTarget;
        public Observable<bool> OnKinematicChangedAsObservable() => onKinematicChanged.AsObservable();

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            targetTween.Update(Time.fixedDeltaTime);
        }

        public void SetHeight(float height)
        {
            this.height = height;
        }

        public void SetTrackingTarget(Transform target, FishingLineBody.LinePoint linePoint = null)
        {
            TrackingTarget = target;
            PinnedPoint = linePoint;
            targetTween.Reset();
        }

        public void SetThrowVelocity(Vector3 velocity, FishingLineBody.LinePoint linePoint = null)
        {
            body.isKinematic = false;
            body.linearVelocity = velocity;
            PinnedPoint = linePoint;

            onKinematicChanged.OnNext(false);
        }

        public void UpdatePosition(FishingLine line)
        {
            if (HasForce)
            {
                if (TrackingTarget != null)
                {
                    Vector3 targetPosition = TrackingTarget.position;
                    transform.position = Vector3.Lerp(transform.position, targetPosition, targetTween.Value);
                }

                if (PinnedPoint != null)
                {
                    line.Body.SetPosition(PinnedPoint.currentIndex, transform.position);

                    int currentIndex = Mathf.Max(1, PinnedPoint.currentIndex);
                    Up = line.Body.GetPosition(currentIndex - 1) - line.Body.GetPosition(currentIndex);
                }
            }
            else
            {
                float visualLength = line.Body.VisualLength;
                float targetHeight = visualLength * (height / line.Length);

                float accumulatedHeight = 0f;
                var linePoints = line.Body.Points;
                for (int i = linePoints.Count - 2; i >= 0; --i)
                {
                    var linePoint = linePoints[i];
                    if (targetHeight < accumulatedHeight + linePoint.segmentLength || i == 0)
                    {
                        Vector3 position = line.Body.GetPosition(i + 1);
                        transform.up = Up = (line.Body.GetPosition(i) - position).normalized;

                        float dist = Mathf.Clamp(targetHeight - accumulatedHeight, 0f, linePoint.segmentLength);
                        transform.position = position + transform.up * dist;
                        break;
                    }

                    accumulatedHeight += linePoint.segmentLength;
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