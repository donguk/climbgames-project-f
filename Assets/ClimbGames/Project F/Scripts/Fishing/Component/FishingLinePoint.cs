using System;
using Unity.VisualScripting;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    // 줄 위의 한 점 (찌, 봉돌, 바늘, 손): 바인드된 Segment 위치를 따라가고, 찌/바늘/물고기가 조인트로 매달림
    // - height: 줄 끝(바늘)에서 잰 줄 길이 (m)
    public class FishingLinePoint : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private float height;
        [SerializeField] private Color gizmoColor = Color.green;

        public Rigidbody Body => body;
        public float Height => height;
        public Vector3 LineDirection { get; private set; }
        public FishingLineBody.Segment Segment { get; private set; }


        void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        public void Initialize()
        {
            Segment = null;
            body.isKinematic = true;
        }

        public void SetHeight(float height)
        {
            this.height = Mathf.Max(0f, height);
        }

        public void BindSegment(FishingLineBody.Segment segment)
        {
            Segment = segment;
        }

        // Segment 위치와 줄 방향(위쪽 점 방향)을 따라감
        public void SetPose(Vector3 position, Vector3 lineDirection)
        {
            if (lineDirection.sqrMagnitude > 1e-8f)
                LineDirection = transform.up = lineDirection.normalized;
            transform.position = position;
        }

        // Segment 가 target 을 따라가는 고정점이 됨 (null 이면 다시 자유 점)
        public void SetTrackingTarget(Transform target)
        {
            if (Segment != null)
                Segment.trackingTarget = target;
        }

        // 현재 포인트에 오브젝트 붙이기
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