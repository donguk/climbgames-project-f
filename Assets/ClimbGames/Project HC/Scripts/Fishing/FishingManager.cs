using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    [AssetPath("FishingManager")]
    public class FishingManager : MonoSingleton<FishingManager>
    {
        [SerializeField] private FishingInputProvider inputProvider;
        [SerializeField] private Transform rodRoot;
        [SerializeField] private Transform baitRoot;
        [SerializeField] private FishingLine fishingLine;

        private FishingPoint fishingPoint;
        private FishingRod fishingRod;
        private FishingBait fishingBait;

        private FishingRodActor rodActor = new FishingRodActor();

        public FishingInputProvider InputProvider => inputProvider;

        protected override void Awake()
        {
            base.Awake();

            inputProvider.CastAction.onPressed += OnCastStarted;
            inputProvider.CastAction.onReleased += OnCastReleased;
            inputProvider.CastAction.onCanceled += OnCastCanceled;
        }

        public void Setup(FishingPoint fishingPoint)
        {
            this.fishingPoint = fishingPoint;
            rodRoot.position = fishingPoint.RodRoot.position;

            if (fishingRod == null)
                fishingRod = AssetManager.Instantiate<FishingRod>("PhysicsRod", rodRoot);

            if (fishingBait == null)
                fishingBait = AssetManager.Instantiate<FishingBait>("FishingBait", baitRoot);

            fishingBait.Setup(fishingRod);
            fishingLine.Setup(fishingRod, fishingBait);

            rodActor.Initialize(fishingRod);
            rodActor.Start(FishingRodAction.Idle, fishingRod.GetCancellationTokenOnDestroy());

            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        void OnCastStarted()
        {
            rodActor.ChangeState(FishingRodAction.Load);
        }

        void OnCastReleased()
        {
            rodActor.ChangeState(FishingRodAction.Cast);
        }

        void OnCastCanceled()
        {
            rodActor.ChangeState(FishingRodAction.Idle);
        }
    }
}