using System;
using R3;
using Unity.VisualScripting;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLinePoint : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private float height;
        [SerializeField] private Color gizmoColor = Color.green;

        private FishingLine line;
        private NormalizedTimer targetTween = new NormalizedTimer(1f);

        public Rigidbody Body => body;
        public float Height => height;
        public Vector3 LineDirection { get; private set; }
        public Transform TrackingTarget { get; private set; }
        public FishingLineBody.LinePoint PinPoint { get; private set; }
        public bool HasTarget => TrackingTarget != null;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            targetTween.Update(Time.fixedDeltaTime);
        }

        public void Initialize(FishingLine line)
        {
            this.line = line;

            body.isKinematic = true;
        }

        public void Initialize(FishingLine line, float height)
        {
            Initialize(line);
            SetHeight(height);
        }

        public void SetHeight(float height)
        {
            this.height = height;
        }

        public void UpdatePosition()
        {
            if (HasTarget)
            {
                Vector3 targetPosition = TrackingTarget.position;
                transform.position = Vector3.Lerp(transform.position, targetPosition, targetTween.Value);

                if (PinPoint != null)
                {
                    // 라인 방향 업데이트
                    int index = Mathf.Max(1, PinPoint.currentIndex);
                    LineDirection = transform.up = line.Body.GetPosition(index - 1) - line.Body.GetPosition(index);

                    // 고정된 점 설정
                    line.Body.SetPosition(PinPoint.currentIndex, transform.position);
                }
            }
            else
            {
                float visualLength = line.Body.VisualLength;
                float targetHeight = visualLength * (height / line.TotalLength);

                float accumulatedHeight = 0f;
                var linePoints = line.Body.Points;
                for (int i = linePoints.Count - 2; i >= 0; --i)
                {
                    var linePoint = linePoints[i];
                    if (targetHeight < accumulatedHeight + linePoint.segmentLength || i == 0)
                    {
                        Vector3 position = line.Body.GetPosition(i + 1);
                        LineDirection = transform.up = (line.Body.GetPosition(i) - position).normalized;

                        float dist = Mathf.Clamp(targetHeight - accumulatedHeight, 0f, linePoint.segmentLength);
                        transform.position = position + transform.up * dist;
                        break;
                    }

                    accumulatedHeight += linePoint.segmentLength;
                }
            }
        }

        public void SetTrackingTarget(Transform target, float duration = 0f, Action onComplete = null)
        {
            TrackingTarget = target;
            targetTween.Reset(duration, onComplete);

            if (target != null)
            {
                // 물리 포인트 추가
                PinPoint = line.Body.AddPinPoint(this);
            }
            else
            {
                // 추가된 물리 포인트 삭제
                line.Body.RemovePinPoint(PinPoint);
                PinPoint = null;
            }
        }

        public void Attach(ILineAttachable attachable)
        {
            attachable.Body.isKinematic = false;

            var joint = attachable.Target.GetComponent<ConfigurableJoint>();
            if (joint == null)
            {
                joint = attachable.Target.AddComponent<ConfigurableJoint>();
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
            attachable.Target.rotation = transform.rotation;
            joint.connectedBody = body;

            attachable.OnAttached(this, joint);
        }

        public void Detach(ILineAttachable attachable)
        {
            if (attachable.Target.TryGetComponent<ConfigurableJoint>(out var joint))
                Destroy(joint);

            attachable.OnDetached(this);
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