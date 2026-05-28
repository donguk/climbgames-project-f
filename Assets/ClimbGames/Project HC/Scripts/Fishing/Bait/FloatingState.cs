using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingBaitStateController
    {
        public class FloatingState : FishingBaitStateBase
        {
            public FloatingState(FishingBait bait, FishingBaitStateController controller) : base(bait, controller)
            {
            }

            protected override void OnEnter()
            {
                Vector3 velocity = Bait.Body.linearVelocity;
                Bait.Body.linearVelocity = new Vector3(velocity.x * 0.5f, velocity.y * 0.05f, velocity.z * 0.5f);
                Bait.Body.angularVelocity *= 0.1f;
                Bait.Body.linearDamping = 5f;

                //Bait.Float.Play();
            }

            protected override void OnFixedUpdate()
            {
                Bait.Body.linearDamping = Mathf.MoveTowards(Bait.Body.linearDamping, 1f, Time.deltaTime * 5f);
            }
        }
    }
}