using UnityEngine;
using R3;
using R3.Triggers;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : FishingEquipment
    {
        [SerializeField] AnimationCurve floatingHeight;
        [SerializeField] Transform modelRoot;
        [SerializeField] Vector2 scaleDistance = new Vector3(3f, 5f);
        [SerializeField] Vector3 maxScale = new Vector3(8f, 8f, 8f);

        private Vector3 originScale = Vector3.one;
        private float waterHeight;
        private Quaternion targetRotation;
        private float rotationSpeed = 2f;
        private Subject<Unit> onFloating = new Subject<Unit>();

        public bool IsFloating => Joint == null;

        public Observable<Unit> OnFloatingAsObservable() => onFloating.AsObservable();

        protected override void Awake()
        {
            base.Awake();
            originScale = modelRoot.localScale;
        }

        void Update()
        {
            // 스케일 적용
            UpdateSacle();

            if (IsFloating)
            {
                // update rotation
                modelRoot.rotation = Quaternion.Slerp(modelRoot.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[FishingBobber] OnTggerEnter: {other.gameObject.name}");
            if (other.gameObject.layer == FishingLayer.Water)
                SetFloating();
        }

        public void SetCastingType(CastingType type)
        {
            Joint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            Joint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        public override void AttachToLine(FishingLinePoint point)
        {
            base.AttachToLine(point);

            Joint.lowAngularXLimit = new SoftJointLimit() { limit = -30f };
            Joint.highAngularXLimit = new SoftJointLimit() { limit = 30f };
            Joint.angularZLimit = new SoftJointLimit() { limit = 30 };
            Joint.angularXDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };
            Joint.angularYZDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };

            Body.excludeLayers = 1 << FishingLayer.Water;
            modelRoot.localRotation = Quaternion.identity;
        }

        public override void CastingThrow(Vector3 velocity)
        {
            Body.excludeLayers = 0;
        }

        void SetFloating()
        {
            Destroy(Joint);

            Vector3 position = transform.position;
            waterHeight = position.y;
            targetRotation = Quaternion.identity;

            Body.isKinematic = true;
            AttachPoint.SetTrackingTarget(transform);

            onFloating.OnNext(Unit.Default);
        }

        void UpdateSacle()
        {
            float dist = Vector3.Distance(transform.position, Framework.MainCamera.transform.position);
            float ratio = Mathf.InverseLerp(scaleDistance.x, scaleDistance.y, dist);
            modelRoot.localScale = Vector3.Lerp(originScale, maxScale, ratio);
        }

        public void ApplyLineTension(float tensionValue)
        {
            Vector3 lineDir = AttachPoint.Up;
            lineDir.y = 0f;
            lineDir.Normalize();

            if (tensionValue > 0f && lineDir != Vector3.zero)
            {
                // 회전축 계산 (당겨지는 방향의 오른쪽 축)
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, lineDir).normalized;
                targetRotation = Quaternion.AngleAxis(80f * tensionValue, tiltAxis);
                rotationSpeed = 3f;
            }
            else
            {
                targetRotation = Quaternion.identity;
                rotationSpeed = 2f;
            }
        }

        public void MovePosition(float amount)
        {
            Vector3 lineDir = AttachPoint.Up;
            lineDir.y = 0f;
            lineDir.Normalize();

            transform.position += lineDir * amount;
        }
    }
}