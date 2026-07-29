using UnityEngine;
using UnityEngine.AI;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform tiltPivot;
        [SerializeField] private Transform rightHandle;
        [SerializeField] private Transform leftHandle;

        private Animator _animator;
        private NavMeshAgent navMeshAgent;
        private Quaternion startPanRotation;

        public Animator Animator => _animator;
        public Transform CameraTarget => tiltPivot;
        public Transform RodHandle => rightHandle;
        public Transform LeftHandle => leftHandle;
        public int BodyLayer { get; private set; }
        public int ArmsLayer { get; private set; }

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            navMeshAgent = GetComponent<NavMeshAgent>();

            BodyLayer = _animator.GetLayerIndex(FishingAnimation.BodyLayer);
            ArmsLayer = _animator.GetLayerIndex(FishingAnimation.ArmsLayer);

            startPanRotation = root.rotation;
        }

        public void UpdateRotation(CharacterRotationInfo rotationInfo)
        {
            root.rotation = startPanRotation * Quaternion.AngleAxis(rotationInfo.pan, Vector3.up);
            tiltPivot.localRotation = Quaternion.AngleAxis(rotationInfo.tilt, Vector3.right);
        }

        public void SetTrigger(string name)
        {
            _animator.SetTrigger(name);
        }

        public void SetParameter(string name, int value)
        {
            _animator.SetInteger(name, value);
        }
    }
}