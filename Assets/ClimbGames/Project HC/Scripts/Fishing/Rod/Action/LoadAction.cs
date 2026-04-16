using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingRodActor
    {
        public class LoadAction : ActionBase
        {
            public LoadAction(FishingRodActor actor) : base(actor)
            {

            }

            protected override void OnUpdate()
            {
                Actor.Target.OnLoading(FishingManager.Instance.InputProvider.CastAction);
            }
        }
    }
}