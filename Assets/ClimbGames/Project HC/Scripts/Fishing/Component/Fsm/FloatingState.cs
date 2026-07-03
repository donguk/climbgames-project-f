using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLineController
    {
        public class FloatingState : FishingLineStateBase
        {
            public FloatingState(FishingLine manager, FishingLineController controller) : base(manager, controller)
            {
            }

            protected override void OnEnter()
            {
                //Vector3 velocity = Handle.Body.linearVelocity;
                //Handle.Body.linearVelocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.05f, velocity.z * 0.5f);
                //Handle.Body.angularVelocity *= 0.1f;
                //Handle.Body.linearDamping = 5f;
            }

            protected override void OnFixedUpdate()
            {
                //Handle.Body.linearDamping = Mathf.MoveTowards(Handle.Body.linearDamping, 1f, Time.deltaTime * 5f);
            }
        }
    }
}