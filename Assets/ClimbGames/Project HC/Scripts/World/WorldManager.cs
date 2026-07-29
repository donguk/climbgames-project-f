using ClimbGames.UI;
using Cysharp.Threading.Tasks;
using R3;
using Unity.Cinemachine;
using UnityEditor.AI;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public enum WorldMode
    {
        Map,
        Fishing,
    }

    public class WorldManager : MonoSingleton<WorldManager>
    {
        [SerializeField] private WorldCamera worldCamera;

        private ReactiveProperty<WorldMode> currentMode = new ReactiveProperty<WorldMode>();

        public ReadOnlyReactiveProperty<WorldMode> CurrentMode => currentMode;

        public async UniTaskVoid TransitionToFishing(FishingSpot spot)
        {
            await (DefaultTransition.Instance as ITransitionHandler).BeginAsync(null);

            await UIManager.Instance.ShowUI<UIPanelFishing>("UIPanelFishing", UILayer.View);
            await FishingPlayerManager.Instance.LocalPlayer.Initialize(spot);

            CameraController.Instance.Brain.UpdateMethod = CinemachineBrain.UpdateMethods.FixedUpdate;
            var camera = CameraController.Instance.ChangeCinemachine($"Fishing", FishingPlayerManager.Instance.LocalPlayer.CameraTarget);
            if (camera.TryGetComponent<CinemachineFollow>(out var cameraFollow))
            {
                Vector3 damping = cameraFollow.TrackerSettings.PositionDamping;
                cameraFollow.TrackerSettings.PositionDamping = Vector3.zero;
                await UniTask.NextFrame();
                cameraFollow.TrackerSettings.PositionDamping = damping;
            }

            currentMode.Value = WorldMode.Fishing;
            (DefaultTransition.Instance as ITransitionHandler).Complete();
        }

        public async UniTaskVoid TransitionToMap()
        {
            await (DefaultTransition.Instance as ITransitionHandler).BeginAsync(null);

            CameraController.Instance.Brain.UpdateMethod = CinemachineBrain.UpdateMethods.SmartUpdate;
            CameraController.Instance.ChangeCinemachine("World", worldCamera.transform);

            FishingPlayerManager.Instance.LocalPlayer.Hide();
            UIManager.Instance.Hide<UIPanelFishing>();

            currentMode.Value = WorldMode.Map;
            (DefaultTransition.Instance as ITransitionHandler).Complete();
        }
    }
}