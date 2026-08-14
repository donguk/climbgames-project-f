
using R3;

namespace ClimbGames.ProjectF
{
    public partial class FishingActorMachine
    {
        public class CatchingState : ActorStateBase
        {
            public CatchingState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.SetTrigger(FishingAnimation.PullOut_Catch);

                Actor.Character.Animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.PullOut_Catch, 0.2f, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Player.MessageBroker.Publish(new FishingEvent.OnPullOut()
                    {
                        hookedFish = Fish,
                        rod = Actor.Rod
                    });
                }).AddTo(this);

                // 애니메이션 완료
                Actor.Character.Animator.OnStateExitAsObservable(FishingAnimation.PullOut_Catch, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Player.MessageBroker.Publish(new FishingEvent.OnFishCatched() { fishData = Player.FishData });
                }).AddTo(this);
            }
        }
    }
}