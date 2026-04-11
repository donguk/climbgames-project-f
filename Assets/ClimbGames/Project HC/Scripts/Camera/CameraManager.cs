using UnityEngine;
using Unity.Cinemachine;

namespace ClimbGames.Project_HC
{
    public class CameraManager : MonoSingleton<CameraManager>
    {
        [SerializeField] private Camera mainCamera;

        [SerializeField] private SerializableDictionary<string, CinemachineCamera> cinemachineCameras;


    }
}