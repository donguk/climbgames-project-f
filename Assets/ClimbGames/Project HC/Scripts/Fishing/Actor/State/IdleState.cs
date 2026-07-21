
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorController
    {
        public class IdleState : FishingActorStateBase
        {
            public IdleState(FishingActor actor, FishController fish, FishingActorController fsm) : base(actor, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                FishingModel.Instance.CastingType.Subscribe(_ =>
                {
                    Actor.ChangeCastingType(_);
                }).AddTo(this);
            }
        }
    }
}