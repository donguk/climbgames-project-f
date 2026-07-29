using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingActor : MonoBehaviour
    {
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingRod fishingRod;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;

        public FishingCharacter Character => fishingCharacter;
        public FishingRod Rod => fishingRod;
        public FishingLine Line => fishingLine;
        public FishingHook Hook => fishingHook;
        public FishingBobber Bobber => fishingBobber;

        public void Initialize(int id, Transform spawnPoint)
        {
            // init id
            fishingLine.Initialize(id);
            fishingBobber.Initialize(id);
            fishingHook.Initialize(id);

            var eulerAngles = spawnPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(spawnPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));

            // 착용
            fishingRod.transform.SetParent(fishingCharacter.RodHandle, false);
            fishingRod.transform.localPosition = Vector3.zero;

            // 라인 생성
            fishingLine.Initialize(fishingRod.PoleTip, 1f, 0.5f);

            // 채비 설정
            fishingLine.BobberPoint.Attach(fishingBobber);
            fishingLine.EndPoint.Attach(fishingHook);
        }
    }
}