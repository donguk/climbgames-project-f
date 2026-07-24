using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingActor : MonoBehaviour
    {
        [SerializeField] private FishingCamera fishingCamera;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingRod fishingRod;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;
        [SerializeField] private FishingHook fishingHook;

        private FishingArea fishingArea;

        public FishingCamera Camera => fishingCamera;
        public FishingWaterArea WaterArea => fishingArea.WaterArea;
        public FishingCharacter Character => fishingCharacter;
        public FishingRod Rod => fishingRod;
        public FishingLine Line => fishingLine;
        public FishingHook Hook => fishingHook;
        public FishingBobber Bobber => fishingBobber;

        public async UniTask Initialize(int id, FishingArea area)
        {
            // init id
            fishingLine.Initialize(id);
            fishingBobber.Initialize(id);
            fishingHook.Initialize(id);

            fishingArea = area;

            var eulerAngles = area.CharacterPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(area.CharacterPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));
            fishingCamera.Initialize();

            // 착용
            fishingRod.transform.SetParent(fishingCharacter.RodHandle, false);
            fishingRod.transform.localPosition = Vector3.zero;

            // 라인 생성
            fishingLine.Initialize(fishingRod.PoleTip, 0.5f, 0.25f);

            // 채비 설정
            fishingLine.BobberPoint.Attach(fishingBobber);
            fishingLine.EndPoint.Attach(fishingHook);

            await UniTask.Delay(100);
        }
    }
}