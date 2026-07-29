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



        //public async void Initialize()
        //{
        //    var go = GameObject.FindGameObjectWithTag("Player");
        //    if (go != null)
        //        player = go.GetComponent<NavMeshAgent>();
        //    
        //    Transform parent = UIManager.Instance.transform.FindChildByName("HUD");
        //    joystick = await AssetManager.InstantiateAsync<VariableJoystick>("PlayerJoystick", parent);
        //}

        //void Update()
        //{
        //    if (joystick != null)
        //    {
        //        Vector3 direction = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        //
        //        if (direction.sqrMagnitude > 0.01f)
        //        {
        //            // 캐릭터 회전
        //            player.transform.rotation = Quaternion.LookRotation(direction);
        //
        //            // NavMesh 위에서 부드럽게 이동
        //            player.Move(direction * 5f * Time.deltaTime);
        //        }
        //    }
        //
        //    if (Keyboard.current != null)
        //    {
        //        if (Keyboard.current.escapeKey.wasReleasedThisFrame)
        //            TransitionToWorld().Forget();
        //    }
        //}

        public async UniTaskVoid TransitionToFishing(FishingSpot spot)
        {
            await (DefaultTransition.Instance as ITransitionHandler).BeginAsync(null);

            await FishingPlayerManager.Instance.LocalPlayer.Initialize(spot);
            await UIManager.Instance.ShowUI<UIPanelFishing>("UIPanelFishing", UILayer.View);

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