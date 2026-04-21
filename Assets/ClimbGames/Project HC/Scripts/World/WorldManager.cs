using ClimbGames.UI;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    public class WorldManager : MonoSingleton<WorldManager>
    {
        private NavMeshAgent player;
        private VariableJoystick joystick;

        public async void Initialize()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
                player = go.GetComponent<NavMeshAgent>();

            Transform parent = UIManager.Instance.transform.FindChildByName("HUD");
            joystick = await AssetManager.InstantiateAsync<VariableJoystick>("PlayerJoystick", parent);
        }

        void Update()
        {
            if (joystick != null)
            {
                Vector3 direction = new Vector3(joystick.Horizontal, 0, joystick.Vertical);

                if (direction.sqrMagnitude > 0.01f)
                {
                    // 캐릭터 회전
                    player.transform.rotation = Quaternion.LookRotation(direction);

                    // NavMesh 위에서 부드럽게 이동
                    player.Move(direction * 5f * Time.deltaTime);
                }
            }

            if (Keyboard.current != null)
            {
                if (Keyboard.current.escapeKey.wasReleasedThisFrame)
                    TransitionToWorld().Forget();
            }
        }

        public async UniTaskVoid TransitionToFishing(FishingPoint fishingPoint)
        {
            var cinemachine = WorldCamera.Instance.ChangeCinemachine($"WorldToFishing", fishingPoint.CharacterPoint);
            await UniTask.NextFrame();

            var activeBlend = WorldCamera.Instance.ActiveBlend;
            if (activeBlend != null)
            {
                float transitionTime = activeBlend.Duration * 0.5f;
                await UniTask.WaitUntil(() => activeBlend.TimeInBlend > transitionTime);
            }
            await (DefaultTransition.Instance as ITransitionHandler).BeginAsync(null);

            Transform character = SetupFishing(cinemachine, fishingPoint);
            WorldCamera.Instance.ChangeCinemachine($"Fishing", character);
            (DefaultTransition.Instance as ITransitionHandler).Complete();
        }

        public async UniTaskVoid TransitionToWorld()
        {
            await (DefaultTransition.Instance as ITransitionHandler).BeginAsync(null);
            WorldCamera.Instance.ChangeCinemachine("World", player.transform);

            SetupWorld();
            (DefaultTransition.Instance as ITransitionHandler).Complete();
        }

        Transform SetupFishing(CinemachineCamera cinemachineCamera, FishingPoint fishingPoint)
        {
            player.gameObject.SetActive(false);
            joystick.gameObject.SetActive(false);
            UIManager.Instance.Hide<UIMenu>();
            FishingManager.Instance.Setup(fishingPoint);
            return FishingManager.Instance.CharacterHead;
        }

        void SetupWorld()
        {
            player.gameObject.SetActive(true);
            joystick.gameObject.SetActive(true);
            FishingManager.Instance.Close();
        }
    }
}