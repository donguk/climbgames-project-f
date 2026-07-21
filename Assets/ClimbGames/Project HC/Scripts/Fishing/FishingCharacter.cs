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
        private int bodyLayer, armsLayer;
        private Vector2 currentTiltLimit;

        public Animator Animator => _animator;
        public Transform RodHandle => rightHandle;
        public Transform LeftHandle => leftHandle;
        public int BodyLayer => bodyLayer;
        public int ArmsLayer => armsLayer;

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();

            bodyLayer = _animator.GetLayerIndex(FishingAnimation.BodyLayer);
            armsLayer = _animator.GetLayerIndex(FishingAnimation.ArmsLayer);
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

        public Animator PlayFullBodyAnimation(string name)
        {
            _animator.Play(name, bodyLayer);
            _animator.SetLayerWeight(armsLayer, 0f);
            return _animator;
        }

        public void SetCastingType(CastingType type)
        {
            _animator.SetInteger(FishingAnimation.CastingType, (int)type);
        }

        public void SetTrigger(string name)
        {
            _animator.SetTrigger(name);
        }
    }
}