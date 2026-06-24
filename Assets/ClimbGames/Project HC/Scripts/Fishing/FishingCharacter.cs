using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform offset;
        [SerializeField] private Transform rightHandle;
        [SerializeField] private Transform leftHandle;
        [SerializeField] private Vector2 tiltLimit = new Vector2(-10, 10);

        private Animator _animator;
        private int fishingLayer;
        private Vector2 currentTiltLimit;

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
            Vector3 targetTiltLimit = camera.TiltLimit;

            // 케스팅 피칭 모드시 캐릭터 틸트(상하) 제한
            if (FishingModel.Instance.IsLimitedTilt)
                targetTiltLimit = tiltLimit;

            currentTiltLimit = Vector3.Lerp(currentTiltLimit, targetTiltLimit, Time.deltaTime * 5f);
            float currentTilt = Mathf.Clamp(camera.CurrentTilt, currentTiltLimit.x, currentTiltLimit.y);
            root.localRotation = Quaternion.Euler(new Vector3(currentTilt, camera.CurrentPan));
        }

        public Animator PlayAnimation(string name)
        {
            _animator.Play(name, fishingLayer);
            return _animator;
        }

        public void SetCastingType(CastingType type)
        {
            _animator.SetInteger(FishingAnimationParam.CastingType, (int)type);
        }

        public void SetTrigger(string name)
        {
            _animator.SetTrigger(name);
        }
    }
}