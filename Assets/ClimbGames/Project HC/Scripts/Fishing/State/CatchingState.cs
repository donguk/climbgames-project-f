
using R3;

namespace ClimbGames.Project_HC
{
    public partial class FishingPlayer
    {
        public class CatchingState : FishingPlayerStateBase
        {
            public CatchingState(FishingPlayer player, Fish fish, FishingPlayer fsm) : base(player, fish, fsm)
            {
            }

            protected override void OnEnter()
            {
                Actor.Character.SetTrigger(FishingAnimation.PullOut_Catch);

                Actor.Character.Animator.OnNormalizedTimeReachedAsObservable(FishingAnimation.PullOut_Catch, 0.2f, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Player.MessageBroker.Publish(new FishingEvent.OnPullOut()
                    {
                        hookedFish = Fish.Target,
                        rod = Actor.Rod
                    });
                }).AddTo(this);

                // 애니메이션 완료
                Actor.Character.Animator.OnStateExitAsObservable(FishingAnimation.PullOut_Catch, Actor.Character.BodyLayer).Subscribe(_ =>
                {
                    Player.MessageBroker.Publish(new FishingEvent.OnFishCatched() { fishData = Fish.Data });
                }).AddTo(this);
            }
        }
    }
}