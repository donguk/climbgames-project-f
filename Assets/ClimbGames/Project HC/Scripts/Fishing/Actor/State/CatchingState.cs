
using System;
using UnityEditor;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class CatchingState : FishingActorStateBase
        {
            public CatchingState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 
                Fish.ChangeState(FishState.Submit);

                // 물고기 조인트 연결
                Fish.Target.AttachToLine(Actor.Line.EndPoint);
                Actor.Line.SetForcedTension(0f, 1f);
                Actor.Line.SetLength(1f, 2f);


                // 애니메이션 플레이.
                Actor.Character.SetTrigger(FishingAnimation.PullOut_Catch);



            }
        }
    }
}