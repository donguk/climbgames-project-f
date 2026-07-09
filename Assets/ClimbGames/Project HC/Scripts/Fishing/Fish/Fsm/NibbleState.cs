using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class NibbleState : FishStateBase
        {
            private FishPattern fishPattern;

            public NibbleState(FishingActor actor, FishController fsm) : base(actor, fsm)
            {
            }

            protected override void OnEnter()
            {
                fishPattern = AssetManager.LoadAsset<FishPattern>("Fish/FishPattern.asset");
                fishPattern.nibble.Reset();

                Actor.Bobber.ApplyBiteMotion(fishPattern.nibble);
            }

            protected override void OnExit()
            {
                AssetManager.Release(fishPattern);
                fishPattern = null;

                Actor.Bobber.ApplyBiteMotion(null);
            }

            protected override void OnUpdate()
            {
                fishPattern.nibble.Update(Time.deltaTime);

                if (fishPattern.nibble.IsCompleted)
                    fsm.ChangeState(FishState.Interest);
            }
        }
    }
}