using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingEquipment : FishingLineObject
    {
        public virtual void Throw(Vector3 velocity)
        {
            DetachFromLine();

            var body = Body;
            body.isKinematic = false;
            body.linearVelocity = velocity;
            body.linearDamping = 0f;
        }
    }
}