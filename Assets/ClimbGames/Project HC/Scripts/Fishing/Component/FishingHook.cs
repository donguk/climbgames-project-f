using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingHook : FishingEquipment
    {
        private LineRenderer lineRenderer;

        protected override void Awake()
        {
            base.Awake();

            lineRenderer = gameObject.GetOrAddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
        }

        void Start()
        {
            FishingModel.Instance.CastingType.Subscribe(_ =>
            {
                SetPinMode(_ == CastingType.Pitching);
            }).AddTo(this);
        }

        void Update()
        {
            if (Joint.connectedBody != null)
            {
                lineRenderer.SetPosition(0, Joint.connectedBody.transform.position);
                lineRenderer.SetPosition(1, transform.position);
            }
        }

        void SetPinMode(bool value)
        {
            Joint.anchor = value ? new Vector3(0f, 0.1f) : Vector3.zero;
            Joint.angularXMotion = value ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
            Joint.angularZMotion = value ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
        }
    }
}