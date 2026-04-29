using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCameraController : MonoBehaviour
    {
        [SerializeField] private Transform pan;
        [SerializeField] private Transform tilt;
        [SerializeField] private Transform target;

        [SerializeField] private float sensitivity = 0.02f;
        [SerializeField] private Vector2 panLimit = new Vector2(-10f, 10f);
        [SerializeField] private Vector2 tiltLimit = new Vector2(-10f, 10f);

        public Transform TrackingTarget => target;
        public Transform Pivot => tilt;

        private float currentPan, currentTilt;

        public void Initialize()
        {
            pan.localRotation = tilt.localRotation = Quaternion.identity;
            currentPan = currentTilt = 0f;
        }

        public void UpdateRotation(Vector2 delta)
        {
            currentPan += delta.x * sensitivity;
            currentTilt -= delta.y * sensitivity;

            currentPan = Mathf.Clamp(currentPan, panLimit.x, panLimit.y);
            currentTilt = Mathf.Clamp(currentTilt, tiltLimit.x, tiltLimit.y);

            pan.localRotation = Quaternion.AngleAxis(currentPan, Vector3.up);
            tilt.localRotation = Quaternion.AngleAxis(currentTilt, Vector3.right);
        }
    }
}