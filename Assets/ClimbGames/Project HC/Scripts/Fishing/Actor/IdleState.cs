
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorStateController
    {
        public class IdleState : FishingActorStateBase
        {
            public IdleState(FishingActor actor, FishingActorStateController controller) : base(actor, controller)
            {
            }

            protected override void OnEnter()
            {
                FishingModel.Instance.CastingType.Subscribe(_ =>
                {
                    Actor.Character.PlayIdle(_);
                    switch (FishingModel.Instance.CastingType.CurrentValue)
                    {
                        case CastingType.Pitching:
                            {
                                Actor.Line.PinEnd(Actor.Character.LeftHandle);
                                break;
                            }
                        case CastingType.OverHead:
                            {
                                Actor.Line.PinEnd(null);
                                break;
                            }
                    }
                }).AddTo(this);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
                Actor.Line.UpdateSegment();
            }
        }
    }
}