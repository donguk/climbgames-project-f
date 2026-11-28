using UnityEngine;
using Cysharp.Threading.Tasks;

namespace ClimbGames.ProjectF
{
    public class FishingPlayer
    {
        private FishingPlayerModel model;
        private FishModel fishModel;

        private FishingSpot spot;
        private FishingActor actor;
        private HookedFish hookedFish;

        private FishingActorMachine actorMachine;
        private FishLifecycleStateMachine fishLifecycle;
        private MessageBroker messageBroker;
        private FishingInputHandler handler;

        public FishingPlayerModel Model => model;

        public FishingSpot Spot => spot;
        public FishingActor Actor => actor;
        public Transform CameraTarget => actor.Character.CameraTarget;
        public HookedFish Fish => hookedFish;
        public FishLifecycleStateMachine FishLifecycle => fishLifecycle;
        public MessageBroker MessageBroker => messageBroker;

        public FishingPlayer(int id)
        {
            fishModel = new FishModel();
            model = new FishingPlayerModel(id, fishModel);
            messageBroker = FishingEvent.GetBroker(id);

            actorMachine = new FishingActorMachine();
            fishLifecycle = new FishLifecycleStateMachine();

            actorMachine.onChange += (_) => model.ChangeActorState(_);
        }

        public virtual async UniTask Initialize(FishingSpot spot)
        {
            this.spot = spot;

            if (actor == null)
                actor = AssetManager.Instantiate<FishingActor>("FishingActor");

            if (handler == null)
                handler = AssetManager.Instantiate<FishingInputHandler>("FishingInputHandler");

            actor.gameObject.SetActive(true);
            await actor.Initialize(this, spot);

            actorMachine.InitializeAndStart(this);
            fishLifecycle.InitializeAndStart(this);

            await UniTask.NextFrame();
        }

        public void Hide()
        {
            if (actor != null)
                actor.gameObject.SetActive(false);

            fishLifecycle.Pause();
            fishLifecycle.Pattern.Pause();
            actorMachine.Pause();
        }

        public void StartCasting(float value)
        {
            messageBroker.Publish(new FishingEvent.OnCastingStarted() { intensity = value });
        }

        // 획득
        public void TakeHookedFish()
        {
            actorMachine.ChangeState(FishingActorState.Idle);
            fishLifecycle.ChangeState(FishLifecycleState.Idle);
        }

        // 물고기 결정: 기본 데이터(FishModel) → 클립 asset 로드(FishPattern). 클립이 하나도 없으면 false
        public bool PrepareFish(int fishId)
        {
            // 이전에 준비한 물고기가 남아 있으면 먼저 해제 (Load 도 시작할 때 해제함)
            fishModel.End();

            fishModel.Initialize(fishId);
            if (fishLifecycle.Pattern.Load(fishModel.Spec) == false)
            {
                Debug.LogError($"[FishingPlayer] 물고기 {fishId} 의 클립을 하나도 불러오지 못했습니다.");
                return false;
            }

            return true;
        }

        public HookedFish CreateHookedFish(FishingHook hook)
        {
            fishModel.Begin();

            if (hookedFish == null)
                hookedFish = AssetManager.Instantiate<HookedFish>("HookedFish");

            hookedFish.transform.position = hook.transform.position;
            hookedFish.Initialize();

            return hookedFish;
        }

        public void DeleteHookedFish()
        {
            if (hookedFish != null)
                GameObject.Destroy(hookedFish.gameObject);

            fishModel.End();
            fishLifecycle.Pattern.Dispose();

            hookedFish = null;
        }
    }
}