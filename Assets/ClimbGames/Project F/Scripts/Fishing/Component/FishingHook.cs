using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingHook : FishingLineObject
    {
        private const float StandardGravity = 9.81f;    // kg 무게 환산 (1 kg = 9.81 N)
        private const float MinPullMass = 0.000001f;
        // 당김 상한 (kg): 바늘 점은 아주 가벼워서(약 0.06g) 서브스텝 변위가 이 값에서 약 5cm, 넘으면 줄 간격보다 크게 튀어 발산할 수 있음
        private const float MaxPullLoad = 0.05f;

        // 물고기가 바늘을 당기는 힘 (입질): 방향(단위벡터)과 세기(kg, 무게 환산)
        private Vector3 pullDirection;
        private float pullLoad;

        public bool IsFlying => RuntimeJoint == null && Body.isKinematic == false;

        // 입질: 바늘 점을 direction 으로 load(kg 무게 환산) 만큼 당김 (음수면 반대로)
        // 줄을 타고 찌까지 전해져 찌가 잠기거나 기울고, 로드 팁도 휨
        public void SetPull(Vector3 direction, float load)
        {
            if (direction.sqrMagnitude < 1e-8f || float.IsFinite(load) == false)
            {
                ClearPull();
                return;
            }

            pullDirection = direction.normalized;
            pullLoad = Mathf.Clamp(load, -MaxPullLoad, MaxPullLoad);
        }

        public void ClearPull()
        {
            pullDirection = Vector3.zero;
            pullLoad = 0f;
        }

        // 서브스텝마다 적분 직후: 당기는 힘을 위치 변화로 (Δx = F / m · Δt², m 은 바늘 + 맡은 줄 질량)
        public override void OnSubStep(FishingLineBody.Segment segment, float deltaTime)
        {
            if (pullLoad == 0f || segment.IsPinned)
                return;

            float acceleration = pullLoad * StandardGravity / Mathf.Max(MinPullMass, segment.mass);
            segment.position += pullDirection * (acceleration * deltaTime * deltaTime);
        }

        // 컴포넌트를 처음 붙일 때 기본값: 가는 철사를 구부린 모양이라 지름은 작고 옆으로 받는 저항이 큼
        void Reset()
        {
            bodyDiameter = 0.002f;
            bodyDragScale = 2f;
            bodyDensity = 7850f;    // 강철
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == FishingLayer.Water)
                Body.linearDamping = 5f;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.layer == FishingLayer.Ground)
                MessageBroker.Publish(new FishingEvent.OnHookGrounded());
        }

        protected override void OnAttachToLine(FishingLinePoint point, ConfigurableJoint joint)
        {
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;
            joint.projectionDistance = 0.1f;

            Body.excludeLayers |= 1 << FishingLayer.Ground;
        }


        // 이벤트 
        protected override void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
            ClearPull();
            DetachFromLine();

            var body = Body;
            body.isKinematic = false;
            body.linearVelocity = param.velocity;
            body.linearDamping = 0f;
            body.excludeLayers &= ~(1 << FishingLayer.Ground);
        }

        protected override void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
            AttachToLine(LinePoint);
        }

        // 물고기가 바늘을 직접 끌고 가거나(입질 성공), 놓거나, 회수하면 입질 당김을 끔
        protected override void OnFishBite(FishingEvent.OnFishBite param)
        {
            ClearPull();
        }

        protected override void OnFishFlee(FishingEvent.OnFishFlee param)
        {
            ClearPull();
        }

        protected override void OnPullOut(FishingEvent.OnPullOut param)
        {
            ClearPull();
        }
    }
}