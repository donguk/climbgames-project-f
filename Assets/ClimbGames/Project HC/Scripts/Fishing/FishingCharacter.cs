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

        public void UpdateRotation(FishingCamera camera, FishingPlayerModel model)
        {
            Vector3 targetTiltLimit = camera.TiltLimit;

            // 케스팅 피칭 모드시 캐릭터 틸트(상하) 제한
            if (model.CastingType.CurrentValue == CastingType.Pitching && model.ActorState.CurrentValue == FishingActorState.Idle)
                targetTiltLimit = tiltLimit;

            currentTiltLimit = Vector3.Lerp(currentTiltLimit, targetTiltLimit, Time.deltaTime * 5f);
            float currentTilt = Mathf.Clamp(camera.CurrentTilt, currentTiltLimit.x, currentTiltLimit.y);
            root.localRotation = Quaternion.Euler(new Vector3(currentTilt, camera.CurrentPan));
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