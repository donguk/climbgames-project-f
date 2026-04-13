using UnityEngine;
using Unity.Cinemachine;

namespace ClimbGames.Project_HC
{
    public class WorldCamera : MonoSingleton<WorldCamera>
    {
        private const int CINEMACHINE_LIVE_PRIORITY = 10;

        [SerializeField] private Camera mainCamera;
        [SerializeField] private SerializableDictionary<string, CinemachineCamera> cinemachineCameras;

        private CinemachineBrain cinemachineBrain;

        public bool IsBlending => cinemachineBrain.IsBlending;
        public CinemachineBlend ActiveBlend => cinemachineBrain.ActiveBlend;

        protected override void Awake()
        {
            base.Awake();

            cinemachineBrain = mainCamera.GetComponent<CinemachineBrain>();
        }

        public CinemachineCamera ChangeCinemachine(string key)
        {
            CinemachineCamera target = null;
            foreach (var pair in cinemachineCameras)
            {
                if (pair.Key == key)
                {
                    target = pair.Value;
                    pair.Value.Priority = CINEMACHINE_LIVE_PRIORITY;
                }
                else
                {
                    pair.Value.Priority = 0;
                }
            }

            return target;
        }

        public CinemachineCamera RestoreCinemachine() => ChangeCinemachine("World");
    }
}