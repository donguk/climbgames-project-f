using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class PhysicsRod : FishingRod
    {

        public override void OnLoading(FishingInput.ICastAction castAction)
        {
            var intensity = castAction.loadIntensity;
            transform.localRotation = Quaternion.AngleAxis(intensity * Mathf.PI * Mathf.Rad2Deg * 0.25f, Vector3.left);
        }
    }
}