
using System;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class CastingState : FishingActorStateBase
        {
            public CastingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 케스팅 애니메이션
                Actor.Character.SetTrigger(FishingAnimation.Throw);
                Actor.Character.Animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.Throw, 0.4f, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Throw();
                }).AddTo(this);

                // (찌 없는 경우) 바늘이 땅에 닿았을 경우
                FishingEvent.Broker.Receive<FishingEvent.OnHookGrounded>().Subscribe(_ => fsm.ChangeState(FishingActorState.Waiting)).AddTo(this);

                // 찌가 물에 닿을 경우
                FishingEvent.Broker.Receive<FishingEvent.OnBobberEnterWater>().Subscribe(_ => fsm.ChangeState(FishingActorState.Waiting)).AddTo(this);
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float dist = Vector3.Distance(Actor.Line.RodTip.position, Actor.Line.EndPoint.transform.position);
                if (Actor.Line.TotalLength < dist)
                    Actor.Line.SetLength(dist);
            }

            public void Throw()
            {
                CastingType castingType = FishingModel.Instance.CastingType.CurrentValue;
                float throwAngle = castingType == CastingType.OverHead ? 40f : 20f;

                if (TryGetThrowVelocity(Actor.Hook.transform, Actor.WaterArea.Center, throwAngle, out var velocity))
                {
                    FishingEvent.Broker.Publish(new FishingEvent.OnCastingThrow() { velocity = velocity });
                }
            }

            bool TryGetThrowVelocity(Transform target, Vector3 position, float angle, out Vector3 result)
            {
                Vector3 direction = position - target.position;
                float h = direction.y; // 높이 차 (yOffset)
                direction.y = 0; // 수평 거리 계산을 위해 y축 제거
                float d = direction.magnitude; // 수평 거리 (distance)
                float a = angle * Mathf.Deg2Rad; // 라디안 변환
                float g = Physics.gravity.magnitude;

                // 포물선 속도 공식 (분모 0 체크 포함)
                float cosA = Mathf.Cos(a);
                float v2 = (g * d * d) / (2 * cosA * cosA * (d * Mathf.Tan(a) - h));

                if (v2 <= 0)
                {
                    Debug.LogWarning("invalid throw angle..");
                    result = Vector3.zero;
                    return false;
                }

                float v = Mathf.Sqrt(v2);

                // 발사 벡터 생성
                // 1. 수평 방향으로 정규화된 벡터
                Vector3 launchVec = direction.normalized;

                // 2. 수평 벡터를 위쪽(Y)으로 '각도'만큼 회전
                // 수평 성분은 cos(a), 수직 성분은 sin(a)의 비율을 가집니다.
                Vector3 finalVelocity = launchVec * cosA;
                finalVelocity.y = Mathf.Sin(a);

                // 3. 최종 속도 크기 곱하기
                result = finalVelocity * v;
                return true;
            }
        }
    }
}