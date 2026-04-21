using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform arm;
        [SerializeField] private Transform hand;
        [SerializeField] private float rotationSensitivity = 0.02f;
        [SerializeField] private Vector2 yawLimit = new Vector2(-10f, 10f);
        [SerializeField] private Vector2 pitchLimit = new Vector2(0f, 10f);

        private Quaternion baseRotation;
        private Quaternion baseHeadRotation;
        private Quaternion baseArmRotation;
        private Quaternion baseHandRotation;
        private float currentYaw, currentPitch;

        public Transform Head => head;
        public Transform Hand => hand;
        public Transform Arm => arm;

        void Awake()
        {
            baseArmRotation = arm.localRotation;
            baseHandRotation = hand.localRotation;
        }

        public void Setup(FishingPoint point)
        {
            Vector3 angle = point.CharacterPoint.rotation.eulerAngles;

            transform.SetPositionAndRotation(point.CharacterPoint.position, Quaternion.Euler(0f, angle.y, 0f));
            head.rotation = point.CharacterPoint.rotation;

            baseRotation = transform.rotation;
            baseHeadRotation = head.localRotation;

            arm.localRotation = baseArmRotation;
            hand.localRotation = baseHandRotation;

            currentYaw = currentPitch = 0f;
        }

        public void UpdateCastgAiming(Vector2 delta, float intensity)
        {
            // 좌우 회전
            currentYaw += delta.x * rotationSensitivity;
            currentYaw = Mathf.Clamp(currentYaw, yawLimit.x, yawLimit.y);
            transform.rotation = baseRotation * Quaternion.AngleAxis(currentYaw, Vector3.up);

            // 상하 회전
            currentPitch = Mathf.InverseLerp(0f, 0.5f, intensity - 0.5f) * pitchLimit.y;
            head.localRotation = baseHeadRotation * Quaternion.AngleAxis(currentPitch, Vector3.right * -1f);

            // arm 회전
            float targetAngle = 30f/*default*/ + (20f/*additional*/ * intensity);
            Quaternion targetRot = baseArmRotation * Quaternion.AngleAxis(targetAngle, Vector3.right * -1f);
            arm.localRotation = Quaternion.Lerp(arm.localRotation, targetRot, Time.deltaTime * 5f);
        }
    }
}