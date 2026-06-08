using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum FishingActorState
    {
        None,
        Idle,               // 아이들 및 낚시 준비 (조작 가능)
        Casting,            // 낚싯대를 휘두르는 캐스팅 동작 중 (조작 불가)
        Waiting,            // 물고기 입질 기다림 (지루한 대기)
        Fighting,           // 물고기와 힘겨루기 (밀당 손맛 루틴)
        CatchSuccess,       // 물고기 잡음 연출 (승리 포즈 및 UI 출력)
        Retrieving,         // 수확 없이 릴링으로 미끼만 회수 중
    }

    public partial class FishingActorStateController : FSM<FishingActorState>
    {
        public abstract class FishingActorStateBase : StateBase
        {
            protected FishingActor Actor { get; private set; }

            protected FishingActorStateBase(FishingActor actor, FishingActorStateController controller) : base(controller)
            {
                Actor = actor;
            }

            protected override void OnFixedUpdate()
            {
                Actor.Character.UpdateRotation(Actor.CameraController);

                Actor.Line.Simulate(Time.fixedDeltaTime);
                Actor.Line.ApplyConstraint();
            }
        }

        public FishingActorStateController() : base("ActorState", true)
        {
        }

        public void InitializeAndStart(FishingActor actor)
        {
            Initialize(
                (FishingActorState.Idle, new IdleState(actor, this)),
                (FishingActorState.Casting, new CastingState(actor, this)),
                (FishingActorState.Waiting, new WaitingState(actor, this)));

            Start(FishingActorState.Idle);
        }
    }
}