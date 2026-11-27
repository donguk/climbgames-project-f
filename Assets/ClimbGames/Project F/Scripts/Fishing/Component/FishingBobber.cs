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

        [Header("Tilt")]
        // 찌가 움직이는 속도와 로드를 흔든 장력 변화 중 큰 쪽으로, 줄 방향(수평)으로 기움
        [SerializeField] private float maxTiltAngle = 60f;                  // 최대 기울기 (도)
        // 움직임: 각도 = maxTiltAngle · (1 − exp(−수평 속도 / tiltMoveSpeed)), 끌려오거나 미끄러질 때 기움
        [SerializeField, Min(0.001f)] private float tiltMoveSpeed = 0.3f;   // 이 속도(m/s)면 최대의 63% 까지 기움
        // 흔들림: 각도 = maxTiltAngle · (1 − exp(−(장력 − tiltShakeTension) / tiltSensitivity)), 찌가 수면 마찰로 멈춰 있어도 기움
        [SerializeField, Min(0f)] private float tiltShakeTension = 0.01f;   // 이 장력(N)을 넘어야 기울기 시작 (공중 줄 무게·idle 흔들림이 만드는 장력보다 크게)
        [SerializeField, Min(0.0001f)] private float tiltSensitivity = 0.03f; // 넘은 양이 이 값(N)이면 최대의 63% 까지 기움, 작을수록 민감
        [SerializeField] private float tiltSpeed = 10f;                     // 기우는 속도
        [SerializeField] private float tiltRecoverSpeed = 0.5f;             // 움직임·장력이 빠진 뒤 서는 속도
        [SerializeField] private float tiltTurnSpeed = 10f;                 // 기울 방향이 줄 방향을 따라가는 속도

        [Header("Buoyancy")]
        // 찌 부력 (kg): 몸통(floatHeight)이 다 잠겼을 때 받칠 수 있는 채비 무게 (봉돌·바늘·줄의 물속 무게), 실제 찌의 "부력 g / 호" 표시와 같음
        // 채비가 이보다 가벼우면 그 비율만큼만 잠기고, 무거우면 가라앉음 (부피 = (찌 질량 + 부력) / 물 밀도)
        [SerializeField, Min(0f)] private float buoyancyLoad = 0.012f;
        // 몸통 높이 (m): 찌 점(몸통 아래 끝)이 이 깊이까지 잠기면 다 잠김, 이 깊이까지는 잠긴 만큼 부력이 커짐
        [SerializeField, Min(0.001f)] private float floatHeight = 0.03f;
        // 잠겼을 때 위아래 움직임을 줄이는 비율 (몸통이 위아래로 물을 가르는 저항, 작으면 수면에서 여러 번 출렁인 뒤 멈춤, 40 ~ 60 이면 거의 출렁이지 않음)
        [SerializeField] private float buoyancyDamping = 14f;

        [Header("Water Hold")]
        // 수면 마찰 (m/s²): 찌를 수평으로 당기는 장력이 질량 · 이 값보다 약하면 제자리에 멈추고, 세면 이만큼 감속하며 끌려감 (처진 줄 무게에 끌려오지 않게)
        // 릴링 장력(몸체·줄 물 저항으로 생김)보다는 작아야 감을 때 끌려옴 → 질량 · waterHold < 릴링 장력
        [SerializeField, Min(0f)] private float waterHold = 5f;

        [Header("Break Out")]
        // 찌 점이 수면보다 이만큼 높이 들리면 물에서 빠짐
        [SerializeField] private float breakoutHeight = 0.25f;

        [Header("Model")]
        [SerializeField] private float modelAlignSpeed = 8f;

        private bool canLand;
        private bool isHolding;
        private Vector3 holdPosition;
        private Quaternion modelRestRotation = Quaternion.identity;
        private float tiltAngle;
        private Vector3 tiltDirection = Vector3.forward;
        private Vector3 lastPointPosition;
        private NormalizedTimer landingTimer = new NormalizedTimer();
        private FishBiteInfo biteInfo;

        public bool IsFloating => RuntimeJoint == null;

        // 수면에 떠 있던 찌 점이 줄에 들려 물에서 빠짐
        public bool ShouldBreakOut => IsFloating && BodyPoint != null
            && (BodyPoint.HasWater == false || BodyPoint.position.y > BodyPoint.waterHeight + breakoutHeight);

        FishingLineBody.Segment BodyPoint => LinePoint != null ? LinePoint.Segment : null;

        // 찌는 재질 밀도(bodyDensity) 대신 부력(buoyancyLoad)으로 부피를 정함: 다 잠겼을 때 찌 질량 + 부력만큼의 물을 밀어냄
        protected override float BodyVolume => (mass + buoyancyLoad) / FishingLineBody.WaterDensity;

        // 찌 점은 몸통 아래 끝: 깊이 0 에서 부력 0, floatHeight 만큼 잠기면 다 잠김
        protected override float BodyHeight => floatHeight;
        protected override float BodyPivot => 0f;

        // 컴포넌트를 처음 붙일 때 기본값: 찌(+ 아래 채비)는 줄보다 수백 배 무거워서 진자처럼 줄을 끌고 다님
        // 부력 12g 찌 (10g 봉돌 채비면 몸통이 약 80% 잠김)
        void Reset()
        {
            mass = 0.008f;
            bodyDiameter = 0.02f;
            buoyancyLoad = 0.012f;
        }

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

            if (IsFloating)
            {
                transform.position = LinePoint.transform.position;
                SimulateRotation(Time.fixedDeltaTime);
                biteInfo.Reset();
            }
            else if (canLand && point.inWater)
            {
                LandOnWater(point);
            }
        }

        // 줄 계산 중 (서브스텝마다 적분 직후): 부력은 적분(FishingLineBody.Integrate)이 bodyDensity 로 처리, 여기서는 위아래 출렁임을 물이 잡아 주고 수면 마찰을 적용
        // 줄이 부력보다 세게 당기면 잠긴 채로 끌려가고, 풀리면 수면으로 떠오름
        public override void OnSubStep(FishingLineBody.Segment segment, float deltaTime)
        {
            float depth = segment.waterHeight - segment.position.y;
            if (segment.IsPinned || segment.HasWater == false || depth <= 0f)
            {
                isHolding = false;
                return;
            }

            float velocityY = (segment.position.y - segment.previous.y) / (1f + buoyancyDamping * deltaTime);
            segment.position.y = segment.previous.y + velocityY;

            ApplyWaterHold(segment, deltaTime);
        }

        // 수면 마찰: 이번 서브스텝 수평 이동(속도 · Δt, 멈춰 있을 때는 지난 풀이에서 줄이 당긴 거리 ≈ 장력 / 질량 · Δt²)을 waterHold · Δt² 와 비교
        // - 작으면 멈춤: 고정 위치로 되돌림 (길이 제약 풀이가 당긴 만큼을 매번 되돌리므로 조금씩 끌려오는 것이 쌓이지 않음)
        // - 크면 미끄러짐: 수평 이동을 waterHold · Δt² 만큼 줄이고 (일정한 감속), 고정 위치를 지금 위치로 옮김
        //   착수할 때 수평 속도도 이 감속으로 줄다가 waterHold · Δt 아래가 되면 멈춤
        void ApplyWaterHold(FishingLineBody.Segment segment, float deltaTime)
        {
            if (waterHold <= 0f)
            {
                isHolding = false;
                return;
            }

            if (isHolding == false)
            {
                isHolding = true;
                holdPosition = segment.previous;
            }

            Vector3 move = segment.position - segment.previous;
            move.y = 0f;
            float limit = waterHold * deltaTime * deltaTime;
            float distance = move.magnitude;
            if (distance > limit)
            {
                segment.position -= move / distance * limit;
                holdPosition = segment.position;
                return;
            }

            // 위아래는 부력이 처리
            segment.position.x = segment.previous.x = holdPosition.x;
            segment.position.z = segment.previous.z = holdPosition.z;
        }

        void LandOnWater(FishingLineBody.Segment point)
        {
            Debug.Log("[FishingBobber] LandOnWater!!!");
            canLand = false;
            landingTimer.Reset(landingClip.duration);

            DetachFromLine();
            Body.excludeLayers = 1 << FishingLayer.Water;

            MessageBroker.Publish(new FishingEvent.OnBobberEnterWater() { waterHeight = point.waterHeight });
        }

        // 줄 방향(수평)으로, 찌가 움직이는 속도와 로드를 흔든 장력 변화 중 큰 쪽만큼 기움
        // 방향과 크기를 따로 따라감: 방향은 줄 방향을 빠르게, 크기는 빠르게 기울고 천천히 회복
        // (한 회전으로 Slerp 하면 처음 당긴 방향으로 기운 뒤 줄 방향이 바뀌어도 회복 속도로만 따라가서 방향이 어긋남)
        void SimulateRotation(float deltaTime)
        {
            // 줄이 거의 수직이면 기울 방향이 없으므로 이전 방향 유지
            Vector3 lineDirection = LinePoint.LineDirection;
            lineDirection.y = 0f;
            if (lineDirection.sqrMagnitude > 1e-6f)
                tiltDirection = Vector3.Slerp(tiltDirection, lineDirection.normalized, tiltTurnSpeed * deltaTime).normalized;

            // 움직임: 찌 점의 수평 속도 (끌려오거나 미끄러질 때)
            Vector3 position = LinePoint.transform.position;
            Vector3 move = position - lastPointPosition;
            move.y = 0f;
            lastPointPosition = position;
            float moveAngle = maxTiltAngle * (1f - Mathf.Exp(-(move.magnitude / deltaTime) / tiltMoveSpeed));

            // 흔들림: tiltShakeTension 을 넘은 장력 (로드를 흔들어 줄이 당겨질 때)
            float tension = BodyPoint != null ? BodyPoint.upperTension : 0f;
            float shakeExcess = Mathf.Max(0f, tension - tiltShakeTension);
            float shakeAngle = maxTiltAngle * (1f - Mathf.Exp(-shakeExcess / tiltSensitivity));

            float targetAngle = Mathf.Max(moveAngle, shakeAngle);
            float speed = targetAngle > tiltAngle ? tiltSpeed : tiltRecoverSpeed;
            tiltAngle = Mathf.Lerp(tiltAngle, targetAngle, speed * deltaTime);

            // 위쪽이 tiltDirection 쪽으로 tiltAngle 만큼 기움
            transform.rotation = Quaternion.AngleAxis(tiltAngle, Vector3.Cross(Vector3.up, tiltDirection));
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
            isHolding = false;
        }

        protected override void OnDetachFromLine(FishingLinePoint point)
        {
            isHolding = false;
            Body.isKinematic = true;

            // 물에 떨어질 때의 기울기에서 이어서 시작함 (장력이 없으면 tiltRecoverSpeed 로 천천히 섬)
            Vector3 up = transform.up;
            tiltAngle = Vector3.Angle(Vector3.up, up);
            up.y = 0f;
            if (up.sqrMagnitude > 1e-6f)
                tiltDirection = up.normalized;

            // 착수 첫 프레임에 움직임 속도가 튀지 않게
            lastPointPosition = point.transform.position;
        }

        // 조인트는 연결 순간의 회전을 기준으로 삼으므로 본체는 바로 줄 방향에 맞추고, 모델은 이전 회전에서 서서히 돌아감
        void ReattachToLine()
        {
            Quaternion previousRotation = transform.rotation;
            AttachToLine(LinePoint);

            if (modelRoot != null)
                modelRoot.rotation = previousRotation * modelRestRotation;
        }

        // 물에서 빠져 줄에 다시 매달림 (찌 점이 다시 수면에 닿으면 착수함)
        public void BreakOut()
        {
            ReattachToLine();
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
            if (RuntimeJoint == null)
                return;

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
            ReattachToLine();
        }

        // 물고기가 도망가면 수면에 다시 뜸 (찌 점이 물에 없으면 WaitingState 에서 다시 매달림)
        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            DetachFromLine();
            landingTimer.Reset(landingClip.duration);
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            canLand = false;
            ReattachToLine();
        }
    }
}
