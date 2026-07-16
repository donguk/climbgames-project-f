using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class StruggleState : FishStateBase
        {
            private HookedFish targetFish;
            private float prevDist;
            private Vector3 hookedPosition;
            private Vector3 targetPosition;

            public StruggleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 생성
                targetFish = Controller.CreateHookedFish();

                hookedPosition = Actor.Hook.transform.position;
                targetPosition = targetFish.transform.position;
                targetPosition.y = 0;

                // 바늘 연결
                Actor.Bobber.AttachToLine(Actor.Line.BobberPoint);
                Actor.Line.HookFish(targetFish);
            }

            protected override void OnUpdate()
            {
                if (targetFish.LifeRate > 0f)
                {
                    //if (Actor.Line.TensionByLength > 1f/*민감도*/)
                    //{
                    //    // 물고기 놓침
                    //}
                }
                else
                {
                    // catched
                }
            }

            protected override void OnFixedUpdate()
            {
                Vector3 position = targetFish.transform.position;
                position.y = 0f;

                float dist = Vector3.Distance(position, targetPosition);
                if (prevDist < dist || dist < 0.1f)
                {
                    float lifeRate = targetFish.LifeRate;
                    Vector3 poleTip = Actor.Rod.PoleTip.position;
                    poleTip.y = 0f;

                    var struggleArea = Actor.WaterArea.GetStruggleArea(hookedPosition + (poleTip - hookedPosition) * (1f - lifeRate), FishData.struggleRange * lifeRate);
                    Vector2 randomCircle = Random.insideUnitCircle * struggleArea.range;
                    targetPosition = struggleArea.center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    targetPosition.y = 0;

                    // 위치 보정
                    if (Vector3.Distance(position, targetPosition) < 0.1f)
                        targetPosition += (targetPosition - position).normalized * 0.1f;

                    targetFish.MovePosition(targetPosition);
                }

                prevDist = dist;
            }
        }
    }
}