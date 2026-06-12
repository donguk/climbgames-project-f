using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingLineHandle : FishingLinePoint
    {
        [SerializeField] private Transform pinnedTarget;

        private FishingLineHandleController controller = new FishingLineHandleController();

        public bool HasForce => pinnedTarget != null || Body.isKinematic == false;
        public Transform Target => pinnedTarget;
        public FishingLineHandleState State => controller.CurrentType;

        public override void Initialize(FishingLine line)
        {
            base.Initialize(line);

            controller.InitializeAndStart(this, FishingLineHandleState.Hanging);
        }

        public void SetTarget(Transform target)
        {
            pinnedTarget = target;

            if (pinnedTarget != null)
                transform.position = pinnedTarget.position;
        }

        public override void UpdatePosition()
        {
            if (pinnedTarget != null)
                transform.position = pinnedTarget.position;

            if (HasForce)
            {
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