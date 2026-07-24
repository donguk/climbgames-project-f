using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public enum FishingActorState
    {
        Idle,
        Casting,            // 캐스팅 동작
        Waiting,            // 기다림
        Fighting,           // 힘겨루기
        Catching,           // 잡음 연출
        Retrieving,         // 낚시대 회수
    }

    public partial class FishingActorMachine : FSM<FishingActorState>
    {
        public abstract class ActorStateBase : StateBase
        {
            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;
            protected HookedFish Fish => Player.Fish;

            protected ActorStateBase(FishingPlayer player, FishingActorMachine fsm) : base(fsm)
            {
                Player = player;
            }

            protected override void OnUpdate()
            {
                // 테스트
                {
                    if (Keyboard.current.cKey.wasReleasedThisFrame)
                        fsm.ChangeState(FishingActorState.Catching);

                    //if (Keyboard.current.fKey.wasReleasedThisFrame)
                    //    Fish.ChangeState(FishState.Flee);
                    //
                    //if (Keyboard.current.bKey.wasReleasedThisFrame)
                    //    Fish.ChangeState(FishState.Bite);
                }


                // 카메라 타켓 회전
                if (FishingInput.Instance.IsPressed)
                    Actor.Camera.UpdateRotation(FishingInput.Instance.DragDelta);
            }

            protected override void OnFixedUpdate()
            {
                // 캐릭터 회전
                Actor.Character.UpdateRotation(Player.Actor.Camera, Player.Model);

                // 라인 업데이트
                Actor.Line.UpdateLine(Time.fixedDeltaTime);
            }
        }

        public FishingActorMachine() : base("<color=green>Actor</color>", true)
        {
        }

        public virtual void InitializeAndStart(FishingPlayer player)
        {
            Initialize(
                (FishingActorState.Idle, new IdleState(player, this)),
                (FishingActorState.Casting, new CastingState(player, this)),
                (FishingActorState.Waiting, new WaitingState(player, this)),
                (FishingActorState.Fighting, new FightingState(player, this)),
                (FishingActorState.Catching, new CatchingState(player, this)),
                (FishingActorState.Retrieving, new RetrievingState(player, this)));

            Start(FishingActorState.Idle);
        }
    }
}