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

                // 바늘 연결
                Actor.Bobber.AttachToLine(Actor.Line.BobberPoint);
                Actor.Line.SetHookedFish(targetFish);
            }

            protected override void OnFixedUpdate()
            {
                var areaInfo = Actor.WaterArea.GetInnerAreaInfo(targetFish.StruggleTarget.position, FishData.struggleRange);
                targetFish.SetAreaInfo(areaInfo);
            }
        }
    }
}