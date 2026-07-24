
using System;
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingActorMachine
    {
        public class RetrievingState : ActorStateBase
        {
            public RetrievingState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.SetTrigger(FishingAnimation.PullOut);

                Actor.Character.Animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.PullOut, 0.2f, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Player.MessageBroker.Publish(new FishingEvent.OnPullOut() { rod = Actor.Rod });
                }).AddTo(this);

                Actor.Character.Animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.PullOut, 0.9f, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    fsm.ChangeState(FishingActorState.Idle);
                }).AddTo(this);
            }
        }
    }
}