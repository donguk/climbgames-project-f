
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingPlayer
    {
        public class IdleState : FishingPlayerStateBase
        {
            public IdleState(FishingPlayer player, Fish fish, FishingPlayer fsm) : base(player, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.SetTrigger(FishingAnimation.Idle);
                Actor.Character.SetParameter(FishingAnimation.CastingType, (int)Player.CastingType);

                Player.MessageBroker.Publish(new FishingEvent.OnCastingTypeChanged() { type = Player.CastingType, leftHandle = Actor.Character.LeftHandle });
                Player.MessageBroker.Receive<FishingEvent.OnCastingStarted>().Subscribe(_ => fsm.ChangeState(FishingPlayerState.Casting)).AddTo(this);
            }
        }
    }
}