using R3;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLineHandle : MonoBehaviour
    {
        [SerializeField] private Transform pinnedTarget;

        private LinearTween targetTween = new LinearTween(0.4f);
        private Subject<bool> onKinematicChanged = new Subject<bool>();

        public bool HasTarget => pinnedTarget != null;
        //public bool HasForce => HasTarget || Body.isKinematic == false;
        //public bool IsKinematic
        //{
        //    get => Body.isKinematic;
        //    set
        //    {
        //        Body.isKinematic = value;
        //        onKinematicChanged.OnNext(Body.isKinematic);
        //    }
        //}
        public Transform Target => pinnedTarget;
        public Observable<bool> OnKinematicChangedAsObservable() => onKinematicChanged.AsObservable();

        void FixedUpdate()
        {
            targetTween.Update(Time.fixedDeltaTime);
        }

        void OnCollisionEnter(Collision collision)
        {
            //Debug.Log($"[FishingLineHandle] OnCollisionEnter: {collision.gameObject.name}");
            //if (collision.gameObject.layer == FishingLayer.Ground)
            //{
            //    //controller.ChangeState(FishingLineHandleState.Settled);
            //}
            //
            //if (collision.gameObject.layer == FishingLayer.Water)
            //{
            //    Body.linearDamping = 1;
            //    Body.linearVelocity *= 0.5f;
            //}
        }

        void OnTriggerEnter(Collider other)
        {
            //Debug.Log($"[FishingLineHandle] OnTggerEnter: {other.gameObject.name}");
            //if (other.gameObject.layer == FishingLayer.Water)
            //{
            //    Body.linearDamping = 1;
            //    Body.linearVelocity *= 0.5f;
            //}
        }

        public void SetTarget(Transform target)
        {
            pinnedTarget = target;
            targetTween.Reset();
        }

        public void UpdatePosition()
        {
            //if (HasForce)
            //{
            //    if (pinnedTarget != null)
            //    {
            //        Vector3 targetPosition = pinnedTarget.position;
            //        transform.position = Vector3.Lerp(transform.position, targetPosition, targetTween.Value);
            //    }
            //
            //    line.SetPosition(line.PositionCount - 1, transform.position);
            //}
            //else
            //{
            //    transform.position = line.GetPosition(line.PositionCount - 1);
            //    transform.up = (line.GetPosition(line.PositionCount - 2) - transform.position).normalized;
            //}
        }

        public void Retrieve()
        {
            //Body.isKinematic = true;
            //onKinematicChanged.OnNext(Body.isKinematic);

            //controller.InitializeAndStart(this, FishingLineHandleState.Hanging);
        }

        public void Drag(float lineTension)
        {
            //Vector3 lineDir = Up;
            //if (Physics.Raycast(transform.position, Vector3.down, out var hit, 0.5f, 1 << FishingLayer.Ground))
            //{
            //    Vector3 surfaceDir = Vector3.ProjectOnPlane(lineDir, hit.normal).normalized;
            //
            //    float verticalScale = Mathf.Abs(lineDir.y);
            //    Vector3 finalDir = Vector3.Slerp(surfaceDir, lineDir, verticalScale).normalized;
            //
            //    float force = 100f * lineTension;
            //    Body.AddForce(finalDir * force, ForceMode.Acceleration);
            //}
            //else
            //{
            //    // 힘 적용 (ForceMode.Acceleration은 질량 무시하고 가속)
            //    float force = 100f * lineTension;
            //    Body.AddForce(lineDir * force, ForceMode.Acceleration);
            //}
        }
    }
}