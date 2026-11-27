using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class FishingActor : MonoBehaviour
    {
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingRod fishingRod;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;
        [SerializeField] private FishingSinker fishingSinker;

        private FishingPlayer player;

        public FishingCharacter Character => fishingCharacter;
        public FishingRod Rod => fishingRod;
        public FishingLine Line => fishingLine;
        public FishingHook Hook => fishingHook;
        public FishingBobber Bobber => fishingBobber;

        public async UniTask Initialize(FishingPlayer player, FishingSpot spot)
        {
            this.player = player;

            // init id
            fishingLine.Initialize(player.Model.ID);
            fishingBobber.Initialize(player.Model.ID);
            fishingHook.Initialize(player.Model.ID);
            if (fishingSinker != null)
                fishingSinker.Initialize(player.Model.ID);

            var eulerAngles = spot.SpawnPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(spot.SpawnPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));

            // 착용
            fishingRod.transform.SetParent(fishingCharacter.RodHandle, false);
            fishingRod.transform.localPosition = Vector3.zero;
            await UniTask.NextFrame();

            // 라인 생성 (로드 팁은 줄 물리에서 스프링에 묶인 질점으로 풂)
            fishingLine.SetRodTip(fishingRod.Bender != null ? fishingRod.Bender : null);
            fishingLine.Initialize(player, fishingRod.PoleTip, fishingHook.transform, 1f);

            // 채비 설정
            fishingBobber.AttachToLine(fishingLine.BobberPoint);
            fishingHook.AttachToLine(fishingLine.EndPoint);
            if (fishingSinker != null)
                fishingSinker.AttachToLine(fishingLine.SinkerPoint);
        }

        void OnDrawGizmos()
        {
            if (player != null && player.Model.ActorState.CurrentValue == FishingActorState.Idle)
            {
                var segmentInfo = player.Spot.GetCastingArea(fishingCharacter.transform);
                if (segmentInfo.IsValid)
                {
                    UnityEditor.Handles.color = Color.red;
                    UnityEditor.Handles.DrawLine(segmentInfo.startPos, segmentInfo.endPos);
                }
            }
        }
    }
}