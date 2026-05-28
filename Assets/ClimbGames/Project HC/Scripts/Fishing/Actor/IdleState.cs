
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public class IdleState : FishingActorStateBase
        {
            private IDisposable disposable;

            public IdleState(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                FishingModel.Instance.CastingType.AsObservable().Subscribe(_ => Actor.Character.PlayIdle(_));
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                switch (FishingModel.Instance.CastingType.CurrentValue)
                {
                    case CastingType.Pitching:
                        {
                            Actor.Line.PinEnd(Actor.Character.LeftHandle);
                            break;
                        }
                }
            }
        }
    }
}