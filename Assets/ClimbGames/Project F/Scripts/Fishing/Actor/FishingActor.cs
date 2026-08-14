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

        private FishingPlayer player;

        public FishingCharacter Character => fishingCharacter;
        public FishingRod Rod => fishingRod;
        public FishingLine Line => fishingLine;
        public FishingHook Hook => fishingHook;
        public FishingBobber Bobber => fishingBobber;

        public void Initialize(FishingPlayer player, FishingSpot spot)
        {
            this.player = player;

            // init id
            fishingLine.Initialize(player.Model.ID);
            fishingBobber.Initialize(player.Model.ID);
            fishingHook.Initialize(player.Model.ID);

            var eulerAngles = spot.SpawnPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(spot.SpawnPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));

            // 착용
            fishingRod.transform.SetParent(fishingCharacter.RodHandle, false);
            fishingRod.transform.localPosition = Vector3.zero;

            // 라인 생성
            fishingLine.Initialize(player, fishingRod.PoleTip, 1f, 0.5f);

            // 채비 설정
            fishingLine.BobberPoint.Attach(fishingBobber);
            fishingLine.EndPoint.Attach(fishingHook);
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