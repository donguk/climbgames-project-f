using UnityEngine;
using Unity.Cinemachine;

namespace ClimbGames.Project_HC
{
    public class CameraController : MonoSingleton<CameraController>
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

        public CinemachineCamera ChangeCinemachine(string key, Transform target)
        {
            CinemachineCamera cinemachine = null;
            foreach (var pair in cinemachineCameras)
            {
                if (pair.Key == key)
                {
                    cinemachine = pair.Value;
                    cinemachine.Target.TrackingTarget = target;
                    pair.Value.Priority = CINEMACHINE_LIVE_PRIORITY;
                }
                else
                {
                    pair.Value.Priority = 0;
                }
            }

            return cinemachine;
        }
    }
}