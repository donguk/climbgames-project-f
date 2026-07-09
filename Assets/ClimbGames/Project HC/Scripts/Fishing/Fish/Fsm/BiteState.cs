using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class BiteState : FishStateBase
        {
            private FishPattern fishPattern;

            public BiteState(FishingActor actor, FishController fsm) : base(actor, fsm)
            {
            }

            protected override void OnEnter()
            {
                fishPattern = AssetManager.LoadAsset<FishPattern>("Fish/FishPattern.asset");
                fishPattern.bite.Reset();

                Actor.Bobber.ApplyBiteMotion(fishPattern.bite);
            }

            protected override void OnExit()
            {
                AssetManager.Release(fishPattern);
                fishPattern = null;

                Actor.Bobber.ApplyBiteMotion(null);
            }

            protected override void OnUpdate()
            {
                fishPattern.bite.Update(Time.deltaTime);

                //if (fishPattern.bite.IsCompleted)
                //    fsm.ChangeState(FishState.Interest);
            }
        }
    }
}