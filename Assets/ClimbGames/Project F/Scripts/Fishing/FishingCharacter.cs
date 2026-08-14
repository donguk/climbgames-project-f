using UnityEngine;
using UnityEngine.AI;

namespace ClimbGames.ProjectF
{
    public class FishingCharacter : MonoBehaviour
    {
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

            startPanRotation = transform.localRotation;
        }

        public void UpdatePosition(Vector3 movement, float deltaTime)
        {
            if (movement.sqrMagnitude > 0.01f)
            {
                movement = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * movement;
                navMeshAgent.Move(movement * 4f * deltaTime);
            }
        }

        public void UpdateRotation(float pan, float tilt)
        {
            transform.localRotation = startPanRotation * Quaternion.AngleAxis(pan, Vector3.up);
            tiltPivot.localRotation = Quaternion.AngleAxis(tilt, Vector3.right);
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