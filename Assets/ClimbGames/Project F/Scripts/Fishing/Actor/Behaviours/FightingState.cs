
using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public partial class FishingActorMachine
    {
        public class FightingState : ActorStateBase
        {
            public FightingState(FishingPlayer player, FishingActorMachine fsm) : base(player, fsm)
            {
            }

            protected override void OnEnter()
            {
                // 데이터 초기화
                FishingPlayerModel model = Player.Model;
                model.lineTension = model.currentTension = 0f;
                model.tensionOverTime = 0f;
                model.slackOverTime = 0f;
                model.isDragging = false;

                // 물고기 도망
                Player.MessageBroker.Receive<FishingEvent.OnFishFlee>().Subscribe(_ => fsm.ChangeState(FishingActorState.Waiting)).AddTo(this);

                // 물고기 잡음
                Player.MessageBroker.Receive<FishingEvent.OnFishSubmit>().Subscribe(_ => fsm.ChangeState(FishingActorState.Catching)).AddTo(this);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();

                FishingPlayerModel model = Player.Model;
                HookedFish hookedFish = Fish;
                FishingRod rod = Actor.Rod;
                if (hookedFish == null)
                    return;

                // 텐션: 줄 물리의 실제 팁 장력(N, 서브스텝 평균) / 줄 강도
                // (solver 가 한 번이라도 NaN 을 내면 currentTension 에 굳어 끊김·드랙 판정이 사라지므로 막음)
                float tipTension = Actor.Line.Body.TipTension;
                float lineTension = float.IsFinite(tipTension) ? tipTension / Mathf.Max(0.1f, rod.BreakForce) : 0f;
                if (model.reelingValue <= 0f)
                    lineTension += Player.GetFishNoise() * hookedFish.Intent.jitter;

                model.lineTension = Mathf.Clamp01(lineTension);
                model.currentTension = Mathf.MoveTowards(model.currentTension, model.lineTension, Time.deltaTime);

                // 끊김 누적: 한계를 넘었거나, 드랙이 풀리는 중에 억지로 감을 때
                bool overPull = model.isDragging && model.reelingValue > 0f;
                if (model.currentTension >= rod.BreakThreshold || overPull)
                    model.tensionOverTime += Time.deltaTime;
                else
                    model.tensionOverTime = 0f;

                // 줄이 느슨한 시간
                if (model.currentTension <= rod.SlackThreshold)
                    model.slackOverTime += Time.deltaTime;
                else
                    model.slackOverTime = 0f;

                // 로드 휨은 줄 물리의 팁 스프링이 실제 장력으로 만듦 (따로 SetLoad 하지 않음)
            }

            protected override void OnExit()
            {
                Actor.Rod.ClearLoad();
                Player.Model.isDragging = false;

                // 파이팅이 끝나면 지난 줄 힘이 남아 물고기를 계속 끌지 않게
                if (Fish != null)
                    Fish.ClearLineForce();
            }

            protected override void OnFixedUpdate()
            {
                base.OnFixedUpdate();

                FishingPlayerModel model = Player.Model;
                HookedFish hookedFish = Fish;
                FishingRod rod = Actor.Rod;
                if (hookedFish == null)
                    return;

                float reelingValue = model.reelingValue;

                // 드랙: 한계를 넘으면 넘은 만큼 줄이 풀리고, 그동안 릴링은 약해짐
                model.isDragging = model.currentTension > rod.DragThreshold;
                if (model.isDragging)
                {
                    float over = (model.currentTension - rod.DragThreshold) / Mathf.Max(0.0001f, 1f - rod.DragThreshold);
                    Actor.Line.Release(rod.DragSpeed * over * Time.fixedDeltaTime);
                    reelingValue *= rod.DragReelScale;
                }

                if (reelingValue > 0f)
                    Actor.Line.Reeling(reelingValue * Time.fixedDeltaTime);

                // 물고기는 줄 끝(바늘) 구간의 실제 장력으로 위쪽 줄 방향으로 끌려옴
                // 늘어난 양 ≈ 장력 · 줄 길이 / 강성 (줄 전체가 같은 장력으로 늘어난다고 봄, 길이는 solver 의 실제 줄 길이)
                FishingLine line = Actor.Line;
                var body = line.Body;
                float endForce = body.GetTension(body.SpanCount - 1);
                float stretch = endForce * Mathf.Max(0.01f, body.Length) / Mathf.Max(0.0001f, line.Stiffness);
                hookedFish.SetLineForce(line.EndPoint.LineDirection, endForce, stretch);
            }
        }
    }
}
