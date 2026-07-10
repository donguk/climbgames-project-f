using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class StruggleState : FishStateBase
        {
            private HookedFish targetFish;

            private Vector3 movePosition;
            private float prevDistance;
            private float moveRange = 1f;

            public StruggleState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                // 물고기 생성
                targetFish = Controller.CreateHookedFish();

                movePosition = targetFish.transform.position;
                movePosition.y = 0;
            }

            protected override void OnFixedUpdate()
            {
                if (targetFish == null)
                    return;

                Vector3 position = targetFish.transform.position;
                position.y = 0f;

                float dist = Vector3.Distance(position, movePosition);
                if (prevDistance < dist || dist < 0.1f)
                {
                    Vector2 randomCircle = Random.insideUnitCircle * moveRange;
                    movePosition = Actor.Area.CastPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    movePosition.y = 0;

                    // 위치 보정
                    if (Vector3.Distance(position, movePosition) < 0.1f)
                        movePosition += (movePosition - position).normalized * 0.1f;

                    targetFish.SetMovePosition(movePosition);
                }

                prevDistance = dist;
            }
        }
    }
}