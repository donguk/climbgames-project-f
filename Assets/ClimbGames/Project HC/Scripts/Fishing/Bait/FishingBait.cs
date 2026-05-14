using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingBait : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private ConfigurableJoint configurableJoint;

        private int locationFlag;
        private FishingBaitStateController stateController = new FishingBaitStateController();

        public FishingBaitState State => stateController.CurrentType;
        public int LocationFlag => locationFlag;
        public Rigidbody Body => body;
        public ConfigurableJoint Joint => configurableJoint;
        public Vector3 Velocity => body.linearVelocity;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (configurableJoint == null)
                configurableJoint = GetComponent<ConfigurableJoint>();

            stateController.InitializeAndStart(this);
        }

        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=green>OnCollisionEnter</color>: {collision.gameObject.name}");
            locationFlag |= 1 << collision.gameObject.layer;

            stateController.ChangeState(FishingBaitState.Floating);
        }

        void OnCollisionExit(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=red>OnCollisionExit</color>: {collision.gameObject.name}");
            locationFlag &= ~(1 << collision.gameObject.layer);
        }

        public void Attach(Transform lineEnd)
        {
            transform.position = lineEnd.position;
            configurableJoint.connectedBody = lineEnd.GetComponent<Rigidbody>();
            locationFlag = 0;

            stateController.ChangeState(FishingBaitState.Hanging);
        }

        public void Throw(Vector3 position, float angle)
        {
            var param = new FishingBaitStateController.FlyingStateParam()
            {
                position = position,
                angle = angle,
            };
            stateController.ChangeState(FishingBaitState.Flying, param);
        }

        public void Drag(Vector3 lineDir, float tensionIntensity)
        {
            if (tensionIntensity <= 0) return;

            if ((locationFlag & 1 << FishingLayer.Ground) != 0 && Physics.Raycast(transform.position, Vector3.down, out var hit, 0.5f, 1 << FishingLayer.Ground))
            {
                Vector3 surfaceDir = Vector3.ProjectOnPlane(lineDir, hit.normal).normalized;

                float verticalScale = Mathf.Abs(lineDir.y);
                Vector3 finalDir = Vector3.Slerp(surfaceDir, lineDir, verticalScale).normalized;

                float force = 100f * tensionIntensity;
                body.AddForce(finalDir * force, ForceMode.Acceleration);
            }
            else if ((locationFlag & 1 << FishingLayer.Water) != 0)
            {
                // 힘 적용 (ForceMode.Acceleration은 질량 무시하고 가속)
                float force = 100f * tensionIntensity;
                body.AddForce(lineDir * force, ForceMode.Acceleration);
            }
        }
    }
}