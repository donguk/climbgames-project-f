using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishingActorState
    {
        Idle,
        Casting,            // 캐스팅 동작 중 (조작 불가)
        WaitForBite,        // 물고기 입질 기다림
        Fighting,           // 물고기와 힘겨루기
        CatchSuccess,       // 물고기 잡음 연출
        Retrieving,         // 릴링으로 미끼 회수
    }

    public partial class FishingActorController : FSM<FishingActorState>
    {
        public abstract class FishingActorStateBase : StateBase
        {
            protected FishingActor Actor { get; private set; }
            protected FishController Fish { get; private set; }

            protected FishingActorStateBase(FishingActor actor, FishController fish, FishingActorController fsm) : base(fsm)
            {
                Actor = actor;
                Fish = fish;
            }

            protected override void OnFixedUpdate()
            {
                // 캐릭터 회전
                Actor.Character.UpdateRotation(Actor.Camera);

                // 라인 업데이트
                Actor.Line.UpdateLine(Time.fixedDeltaTime);
            }
        }

        private FishController fishController = new FishController();

        public FishingActorController() : base("Actor", true)
        {
        }

        public void InitializeAndStart(FishingActor actor)
        {
            Initialize(
                (FishingActorState.Idle, new IdleState(actor, fishController, this)),
                (FishingActorState.Casting, new CastingState(actor, fishController, this)),
                (FishingActorState.WaitForBite, new WaitForBiteState(actor, fishController, this)));

            Start(FishingActorState.Idle);

            fishController.InitializeAndStart(actor);
        }

        public override void Pause()
        {
            base.Pause();
            fishController.Pause();
        }

        public override void Resume()
        {
            base.Resume();
            fishController.Resume();
        }
    }
}