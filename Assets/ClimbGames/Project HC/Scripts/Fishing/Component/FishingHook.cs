using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingHook : FishingEquipment
    {
        private LineRenderer lineRenderer;
        private FishingLinePoint lineHandle;

        protected override void Awake()
        {
            base.Awake();

            lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
        }

        void Update()
        {
            // darw line
            if (Joint != null && Joint.connectedBody != null)
            {
                lineRenderer.SetPosition(0, Joint.connectedBody.transform.position);
                lineRenderer.SetPosition(1, transform.position);
            }
        }

        public void UpdatePosition()
        {
            if (Joint != null && Joint.connectedBody != null)
            {
                // 조인트에 의해 연결
            }
            else if (lineHandle != null) // 케스팅 중
            {
                transform.position = lineHandle.transform.position;
                transform.up = lineHandle.Up;
            }
        }

        public void SetCastingType(CastingType type)
        {
            Joint.anchor = type == CastingType.Pitching ? new Vector3(0f, 0.1f) : Vector3.zero;
            Joint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
            Joint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
        }

        public override void Attach(FishingLinePoint point)
        {
            base.Attach(point);

            Body.isKinematic = false;
            lineHandle = point;
        }

        public void CastingThrow()
        {
            Joint.connectedBody = null;
            Body.isKinematic = true;
        }
    }
}