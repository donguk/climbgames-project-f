using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLineHandle : FishingLinePoint
    {
        [SerializeField] private Transform pinnedTarget;

        private LinearBlender targetBlender = new LinearBlender(0.4f);
        private FishingLineHandleController controller = new FishingLineHandleController();

        public bool HasTarget => pinnedTarget != null;
        public bool HasForce => HasTarget || Body.isKinematic == false;
        public Transform Target => pinnedTarget;
        public FishingLineHandleState State => controller.CurrentType;

        void FixedUpdate()
        {
            targetBlender.Update(Time.fixedDeltaTime);
        }

        public override void Initialize(FishingLine line)
        {
            base.Initialize(line);

            controller.InitializeAndStart(this, FishingLineHandleState.Hanging);
        }

        public void SetTarget(Transform target)
        {
            pinnedTarget = target;
            targetBlender.Reset();
        }

        public override void UpdatePosition()
        {
            if (HasForce)
            {
                if (pinnedTarget != null)
                {
                    Vector3 targetPosition = pinnedTarget.position;
                    transform.position = Vector3.Lerp(transform.position, targetPosition, targetBlender.Value);
                }

                line.SetPosition(line.PositionCount - 1, transform.position);
            }
            else
            {
                Vector3 position = line.GetPosition(line.PositionCount - 1);
                transform.up = (line.GetPosition(line.PositionCount - 2) - position).normalized;
                transform.position = position;
            }
        }
    }
}