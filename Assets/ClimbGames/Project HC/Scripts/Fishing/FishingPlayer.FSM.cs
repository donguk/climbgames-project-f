using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public enum FishingPlayerState
    {
        Idle,
        Casting,            // 캐스팅 동작
        Waiting,            // 기다림
        Fighting,           // 힘겨루기
        Catching,           // 잡음 연출
        Retrieving,         // 낚시대 회수
    }

    public partial class FishingPlayer : FSM<FishingPlayerState>
    {
        public abstract class FishingPlayerStateBase : StateBase
        {
            protected FishingPlayer Player { get; private set; }
            protected FishingActor Actor => Player.Actor;
            protected Fish Fish { get; private set; }

            protected FishingPlayerStateBase(FishingPlayer player, Fish fish, FishingPlayer fsm) : base(fsm)
            {
                Player = player;
                Fish = fish;
            }

            protected override void OnUpdate()
            {
                // 테스트
                {
                    if (Keyboard.current.cKey.wasReleasedThisFrame)
                        fsm.ChangeState(FishingPlayerState.Catching);

                    if (Keyboard.current.fKey.wasReleasedThisFrame)
                        Fish.ChangeState(FishState.Flee);

                    if (Keyboard.current.bKey.wasReleasedThisFrame)
                        Fish.ChangeState(FishState.Bite);
                }


                // 카메라 타켓 회전
                if (FishingInput.Instance.IsPressed)
                    Actor.Camera.UpdateRotation(FishingInput.Instance.DragDelta);
            }

            protected override void OnFixedUpdate()
            {
                // 캐릭터 회전
                Actor.Character.UpdateRotation(Player);

                // 라인 업데이트
                Actor.Line.UpdateLine(Time.fixedDeltaTime);
            }
        }

        protected virtual void InitializeAndStart(FishingPlayer player, Fish fish)
        {
            Initialize(
                (FishingPlayerState.Idle, new IdleState(player, fish, this)),
                (FishingPlayerState.Casting, new CastingState(player, fish, this)),
                (FishingPlayerState.Waiting, new WaitingState(player, fish, this)),
                (FishingPlayerState.Fighting, new FightingState(player, fish, this)),
                (FishingPlayerState.Catching, new CatchingState(player, fish, this)),
                (FishingPlayerState.Retrieving, new RetrievingState(player, fish, this)));

            Start(FishingPlayerState.Idle);
        }
    }
}