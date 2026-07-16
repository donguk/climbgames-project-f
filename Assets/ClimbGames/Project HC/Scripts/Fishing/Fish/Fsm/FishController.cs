using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishState
    {
        Idle,
        Approach,           // 물고기
        Nibble,             // 입질 단계
        Bite,               // 바늘을 물었음
        Struggle,
        Flee,
    }

    public partial class FishController : FSM<FishState>
    {
        public class FishStateBase : StateBase
        {
            protected FishController Controller { get; private set; }
            protected FishingActor Actor => Controller.actor;
            protected FishData FishData => Controller.targetData;
            protected HookedFish HookedFish => Controller.targetFish;

            public FishStateBase(FishController fsm) : base(fsm)
            {
                Controller = fsm;
            }
        }

        private FishingActor actor;
        private FishData targetData;
        private HookedFish targetFish;

        public bool IsHooked => targetFish != null;

        public FishController() : base("Fish", true)
        {
        }

        public void InitializeAndStart(FishingActor actor)
        {
            this.actor = actor;

            Initialize(
                (FishState.Idle, new IdleState(this)),
                (FishState.Approach, new ApproachState(this)),
                (FishState.Nibble, new NibbleState(this)),
                (FishState.Bite, new BiteState(this)),
                (FishState.Struggle, new StruggleState(this)));

            Start(FishState.Idle);
        }

        public void FetchData()
        {
            if (targetData == null)
                targetData = AssetManager.LoadAsset<FishData>("Fish/FishData.asset");
        }

        public HookedFish CreateHookedFish()
        {
            if (targetFish == null)
                targetFish = AssetManager.Instantiate<HookedFish>("HookedFish");

            targetFish.transform.position = actor.Hook.transform.position;
            Vector3 dir = actor.transform.position - actor.Hook.transform.position;
            dir.y = 0f;
            targetFish.transform.forward = dir;

            targetFish.Initialize(this, targetData);
            return targetFish;
        }
    }
}