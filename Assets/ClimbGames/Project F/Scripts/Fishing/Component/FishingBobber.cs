using UnityEngine;
using R3;
using System;

namespace ClimbGames.ProjectF
{
    // 찌는 줄의 찌 점을 따라감
    // - 줄에 매달림 (공중, 입질/싸움): 조인트로 찌 점 위치의 bobberPoint 에 매달림
    // - 수면 (착수 후): 조인트를 풀고 찌 점 위치로 움직임
    // 찌 점은 줄을 만들 때부터 있고 무게가 항상 같아서, 모드가 바뀌어도 줄이 튀지 않음
    public class FishingBobber : FishingLineObject
    {
        [Serializable]
        public class FloatingClip
        {
            public AnimationCurve height;
            public float duration;
        }

        [SerializeField] private Transform modelRoot;
        [SerializeField] private FloatingClip landingClip;
        [SerializeField] private FloatingClip floatingClip;

        [Header("Line Pull")]
        // 찌 점이 길이 제약에 끌리는 정도 (작을수록 무거움, 공중과 수면 모두 같은 값)
        [SerializeField, Range(0f, 1f)] private float lineWeight = 0.2f;
        // 정지 마찰: 물에서 이보다 약하게 끌리면 움직이지 않음
        [SerializeField] private float pullDeadSpeed = 0f;
        // 정지 마찰을 넘어 끌릴 때 물이 잡는 정도 (클수록 천천히 따라옴, 0 이면 넘는 만큼 그대로 움직임)
        [SerializeField] private float pullDrag = 5f;

        // 끌리는 속도를 기울기 비율로 바꾸는 배율 (속도 x 배율 이 1 이면 줄 방향까지 완전히 기움)
        [SerializeField] private float tiltScale = 1f;
        [SerializeField] private float tiltSpeed = 10f;
        [SerializeField] private float tiltRecoverSpeed = 0.5f;

        [Header("Buoyancy")]
        // 몸통이 다 잠겼을 때 위로 미는 가속도 (중력 크기보다 커야 뜸, 클수록 세게 당겨도 잘 잠기지 않음)
        [SerializeField] private float buoyancy = 30f;
        // 잠겼을 때 위아래 움직임을 줄이는 비율 (작으면 수면에서 여러 번 출렁인 뒤 멈춤, 40 ~ 60 이면 거의 출렁이지 않음)
        [SerializeField] private float buoyancyDamping = 14f;

        [Header("Break Out")]
        // 찌 점이 수면보다 이만큼 높이 들리면 물에서 빠짐
        [SerializeField] private float breakoutHeight = 0.25f;
        // 수면이 찌를 잡는 힘: 줄이 위로 당기는 속도에서 빼는 값 (클수록 세게 당겨야 들림)
        [SerializeField] private float surfaceHold = 0.5f;

        [Header("Model")]
        [SerializeField] private float modelAlignSpeed = 8f;

        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private float biteMaxAngle = 90f;

        private bool canLand;
        private Vector3 pullVelocity;
        private Quaternion modelRestRotation = Quaternion.identity;
        private Quaternion currentDragRotation = Quaternion.identity;
        private Quaternion currentBiteRotation = Quaternion.identity;
        private NormalizedTimer landingTimer = new NormalizedTimer();
        private FishBiteInfo biteInfo;

        public bool IsFloating => RuntimeJoint == null;

        // 수면에 떠 있던 찌 점이 줄에 들려 물에서 빠짐
        public bool ShouldBreakOut => IsFloating && BodyPoint != null && BodyPoint.inWater == false;

        FishingLineBody.LinePoint BodyPoint => LinePoint != null ? LinePoint.BodyPoint : null;

        // 찌는 줄의 점을 고정점으로 추적하지 않고, 줄의 찌 점을 따라감
        protected override bool TracksLinePoint => false;

        void Awake()
        {
            if (modelRoot != null)
                modelRestRotation = modelRoot.localRotation;
        }

        void Update()
        {
            if (modelRoot != null)
                modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation, modelRestRotation, modelAlignSpeed * Time.deltaTime);
        }

        void FixedUpdate()
        {
            var point = BodyPoint;
            if (point == null)
                return;

            // 찌 점 속성 (인스펙터에서 바꾼 값이 바로 반영되게 매번 설정함)
            point.weight = lineWeight;
            point.waterFriction = pullDeadSpeed;
            point.waterDrag = pullDrag;
            point.leaveHeight = breakoutHeight;
            point.surfaceHold = surfaceHold;
            point.buoyancy = buoyancy;
            point.buoyancyDamping = buoyancyDamping;
            // 입질로 찌가 잠기는 높이 (기존처럼 꺼 둠)
            // point.surfaceOffset = IsFloating ? biteInfo.bobberHeight : 0f;

            if (IsFloating)
            {
                FollowPoint(Time.fixedDeltaTime);
                SimulateRotation();
                biteInfo.Reset();
            }
            else if (canLand && point.inWater)
            {
                LandOnWater(point);
            }
        }

        void LandOnWater(FishingLineBody.LinePoint point)
        {
            Debug.Log("[FishingBobber] LandOnWater!!!");
            canLand = false;
            landingTimer.Reset(landingClip.duration);

            LinePoint.Detach(this);
            Body.excludeLayers = 1 << FishingLayer.Water;

            MessageBroker.Publish(new FishingEvent.OnBobberEnterWater() { waterHeight = point.waterHeight });
        }

        // 찌 점 위치로 움직이고, 움직인 수평 속도를 기울기에 씀
        void FollowPoint(float deltaTime)
        {
            Vector3 position = LinePoint.transform.position;

            Vector3 move = position - transform.position;
            move.y = 0f;
            pullVelocity = move / deltaTime;

            transform.position = position;
        }

        void SimulateRotation()
        {
            Quaternion dargRotation = Quaternion.identity;
            Vector3 lineDirection = LinePoint.LineDirection;

            // 줄에 끌리는 속도만큼 기울임
            float tiltAmount = pullVelocity.magnitude;

            // 줄 방향이 없으면 기울이지 않음
            if (tiltAmount > 0.0001f && lineDirection.sqrMagnitude > 0.0001f)
            {
                // 찌의 up 에서 줄 방향까지 끌리는 속도에 비례해서 기울임
                Vector3 targetUp = Vector3.Slerp(Vector3.up, lineDirection, Mathf.Clamp01(tiltAmount * tiltScale));
                dargRotation = Quaternion.FromToRotation(Vector3.up, targetUp);
            }

            // 줄에 끌린 기울임은 빠르게 기울고 천천히 회복함
            bool isTilting = Quaternion.Angle(Quaternion.identity, dargRotation) > Quaternion.Angle(Quaternion.identity, currentDragRotation);
            float dragSpeed = isTilting ? tiltSpeed : tiltRecoverSpeed;
            currentDragRotation = Quaternion.Slerp(currentDragRotation, dargRotation, dragSpeed * Time.fixedDeltaTime);
            transform.rotation = currentDragRotation;
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.angularXDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };
            joint.angularYZDrive = new JointDrive() { positionDamper = 1, maximumForce = float.MaxValue };

            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;

            Body.excludeLayers = 1 << FishingLayer.Water;
        }

        protected override void OnDetachFromLine(FishingLinePoint point)
        {
            Body.isKinematic = true;
            pullVelocity = Vector3.zero;

            // 물에 떨어질 때의 회전에서 이어서 시작함
            currentDragRotation = Quaternion.identity;
            currentBiteRotation = transform.rotation;
        }

        // 조인트는 연결 순간의 회전을 기준으로 삼으므로 본체는 바로 줄 방향에 맞추고, 모델은 이전 회전에서 서서히 돌아감
        void AttachToLine()
        {
            Quaternion previousRotation = transform.rotation;
            LinePoint.Attach(this);

            if (modelRoot != null)
                modelRoot.rotation = previousRotation * modelRestRotation;
        }

        // 물에서 빠져 줄에 다시 매달림 (찌 점이 다시 수면에 닿으면 착수함)
        public void BreakOut()
        {
            AttachToLine();
            canLand = true;

            Debug.Log("[FishingBobber] BreakOut");
        }

        public void SetBiteInfo(FishBiteInfo biteInfo)
        {
            this.biteInfo = biteInfo;
        }


        // 이벤트
        protected override void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
            RuntimeJoint.angularXMotion = param.type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            RuntimeJoint.angularZMotion = param.type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }

        protected override void OnCastingThrow(FishingEvent.OnCastingThrow onThrow)
        {
            canLand = true;

            RuntimeJoint.angularXMotion = ConfigurableJointMotion.Locked;
            RuntimeJoint.angularZMotion = ConfigurableJointMotion.Locked;

            Body.excludeLayers = 0;
        }

        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            canLand = false;
            AttachToLine();
        }

        // 물고기가 도망가면 수면에 다시 뜸 (찌 점이 물에 없으면 WaitingState 에서 다시 매달림)
        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            LinePoint.Detach(this);
            landingTimer.Reset(landingClip.duration);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            canLand = false;
            AttachToLine();
        }
    }
}
