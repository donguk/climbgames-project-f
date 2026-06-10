using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCamera : MonoBehaviour
    {
        [SerializeField] private Transform pan;
        [SerializeField] private Transform tilt;
        [SerializeField] private Transform target;

        [SerializeField] private float sensitivity = 0.02f;
        [SerializeField] private Vector2 panLimit = new Vector2(-10f, 10f);
        [SerializeField] private Vector2 tiltLimit = new Vector2(-10f, 10f);

        public float CurrentPan { get; private set; }
        public float CurrentTilt { get; private set; }

        public Transform TrackingTarget => target;
        public Vector2 TiltLimit => tiltLimit;

        public void Initialize()
        {
            pan.localRotation = tilt.localRotation = Quaternion.identity;
            CurrentPan = CurrentTilt = 0f;
        }

        public void UpdateRotation(Vector2 delta)
        {
            CurrentPan += delta.x * sensitivity;
            CurrentTilt -= delta.y * sensitivity;

            CurrentPan = Mathf.Clamp(CurrentPan, panLimit.x, panLimit.y);
            CurrentTilt = Mathf.Clamp(CurrentTilt, tiltLimit.x, tiltLimit.y);

            pan.localRotation = Quaternion.AngleAxis(CurrentPan, Vector3.up);
            tilt.localRotation = Quaternion.AngleAxis(CurrentTilt, Vector3.right);
        }
    }
}