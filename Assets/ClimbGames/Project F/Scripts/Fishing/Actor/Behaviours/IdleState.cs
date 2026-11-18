
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.ProjectF
{
    public partial class FishingActorMachine
    {
        public class IdleState : ActorStateBase
        {
            public IdleState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                FishingPlayerModel model = Player.Model;
                model.CastingType.Subscribe(_ => ChangeCastingMotion(_)).AddTo(this);

                Actor.Character.SetTrigger(FishingAnimation.Idle);
                Player.MessageBroker.Receive<FishingEvent.OnCastingStarted>().Subscribe(_ => fsm.ChangeState(FishingActorState.Casting)).AddTo(this);
            }

            void ChangeCastingMotion(CastingType type)
            {
                Actor.Character.SetParameter(FishingAnimation.CastingType, (int)type);
                Player.MessageBroker.Publish(new FishingEvent.OnCastingTypeChanged() { type = type, leftHandle = Actor.Character.LeftHandle });
            }


            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                float reelingValue = Player.Model.reelingValue;
                if (reelingValue > 0f)
                {
                    if (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed)
                    {
                        Actor.Line.Release(reelingValue * Time.fixedDeltaTime);
                    }
                    else
                    {
                        // 릴 감기
                        if (Actor.Line.TotalLength > 1f)
                            Actor.Line.Reeling(reelingValue * Time.fixedDeltaTime);
                    }
                }
            }
        }
    }
}