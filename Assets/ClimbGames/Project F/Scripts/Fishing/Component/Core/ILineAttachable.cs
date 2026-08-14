using UnityEngine;

namespace ClimbGames.ProjectF
{
    public interface ILineAttachable
    {
        public Transform Target { get; }
        public Rigidbody Body { get; }
        public void OnAttached(FishingLinePoint point, ConfigurableJoint joint);
        public void OnDetached(FishingLinePoint point);
    }
}