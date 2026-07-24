
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorMachine
    {
        public class IdleState : ActorStateBase
        {
            public IdleState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                FishingPlayerModel model = Player.Model;
                model.CastingType.Subscribe(_ => ChangeCastingMotion(_)).AddTo(this);

                Actor.Character.SetTrigger(FishingAnimation.Idle);
                Player.MessageBroker.Receive<FishingEvent.OnCastingStarted>().Subscribe(_ => fsm.ChangeState(FishingActorState.Casting)).AddTo(this);
            }

            void ChangeCastingMotion(CastingType type)
            {
                Actor.Character.SetParameter(FishingAnimation.CastingType, (int)type);
                Player.MessageBroker.Publish(new FishingEvent.OnCastingTypeChanged() { type = type, leftHandle = Actor.Character.LeftHandle });
            }
        }
    }
}