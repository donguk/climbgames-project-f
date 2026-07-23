using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishController
    {
        public class SubmitState : FishStateBase
        {
            public SubmitState(FishController fsm) : base(fsm)
            {
            }

            protected override void OnEnter()
            {
                var hookedFish = HookedFish;

                hookedFish.transform.position = hookedFish.ModelRoot.position;
                hookedFish.ModelRoot.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                FishingEvent.Broker.Publish(new FishingEvent.OnFishSubmit() { hookedFish = hookedFish });
            }
        }
    }
}