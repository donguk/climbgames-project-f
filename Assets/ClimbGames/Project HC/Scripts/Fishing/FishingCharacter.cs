using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform offset;
        [SerializeField] private Transform rightHandle;
        [SerializeField] private Transform leftHandle;
        [SerializeField] private Vector2 pitchingTiltLimit = new Vector2(-10, 10);

        private Animator _animator;
        private int fishingLayer;
        private Vector2 tiltLimit;

        public Transform RodHandle => rightHandle;
        public Transform LeftHandle => leftHandle;
        public AnimatorStateInfo CurrentAnimatorStateInfo => _animator.GetCurrentAnimatorStateInfo(fishingLayer);

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            fishingLayer = _animator.GetLayerIndex(FishingAnimation.LayerName);
        }

        public void UpdateRotation(FishingCamera camera)
        {
            Vector3 targetTilt = camera.TiltLimit;
            if (FishingModel.Instance.CastingType.CurrentValue == CastingType.Pitching && FishingActor.Instance.State == FishingActorState.Idle)
                targetTilt = pitchingTiltLimit;

            tiltLimit = Vector3.Lerp(tiltLimit, targetTilt, Time.deltaTime * 5f);
            float currentTilt = Mathf.Clamp(camera.CurrentTilt, tiltLimit.x, tiltLimit.y);
            root.localRotation = Quaternion.Euler(new Vector3(currentTilt, camera.CurrentPan));
        }

        public Animator PlayAnimation(string name)
        {
            _animator.Play(name, fishingLayer);
            return _animator;
        }

        public void SetCastingType(CastingType type)
        {
            _animator.SetInteger("CastingType", (int)type);
        }
    }
}