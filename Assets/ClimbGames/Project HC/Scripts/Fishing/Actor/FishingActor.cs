using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingActor")]
    public class FishingActor : MonoSingleton<FishingActor>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private FishingCamera fishingCamera;
        [SerializeField] private FishingCharacter fishingCharacter;
        [SerializeField] private FishingLine fishingLine;
        [SerializeField] private FishingBobber fishingBobber;

        private FishingPoint fishingPoint;
        private FishingRod fishingRod;
        private FishingBait fishingBait;
        private FishingActorStateController stateController = new FishingActorStateController();

        public FishingInputProvider Input => inputProvider;
        public FishingCamera Camera => fishingCamera;
        public FishingPoint Point => fishingPoint;
        public FishingCharacter Character => fishingCharacter;
        public FishingBait Bait => fishingBait;
        public FishingLine Line => fishingLine;
        public FishingActorState State => stateController.CurrentType;

        void Update()
        {
            if (inputProvider.CastingInput.isPressed)
                fishingCamera.UpdateRotation(inputProvider.CastingInput.delta);

            //if (Mouse.current != null)
            //{
            //    if (Mouse.current.leftButton.wasReleasedThisFrame)
            //    {
            //        if (EventSystem.current.IsPointerOverGameObject() == false)
            //            StartCasting();
            //    }
            //}
        }

        public async UniTask Initialize(FishingPoint point)
        {
            fishingPoint = point;

            var eulerAngles = point.CharacterPoint.rotation.eulerAngles;
            transform.SetPositionAndRotation(point.CharacterPoint.position, Quaternion.AngleAxis(eulerAngles.y, Vector3.up));
            fishingCamera.Initialize();

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", fishingCharacter.RodHandle);

            //if (fishingBait == null)
            //    fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", transform);

            // 
            fishingLine.BuildLine(fishingRod.PoleTip, 0.5f);
            fishingLine.SetBobberHeight(0.25f);
            fishingBobber.Attach(fishingLine.Bobber);

            gameObject.SetActive(true);
            await UniTask.NextFrame();
            stateController.InitializeAndStart(this);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void StartCasting()
        {
            if (stateController.CurrentType == FishingActorState.Idle)
                stateController.ChangeState(FishingActorState.Casting);
        }
    }
}