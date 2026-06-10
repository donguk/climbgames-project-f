using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform root;
        [SerializeField] private Transform offset;
        [SerializeField] private SerializableDictionary<string, Transform> offsetValues;
        [SerializeField] private Transform rightHandle;
        [SerializeField] private Transform leftHandle;
        [SerializeField] private Vector2 pitchingTiltLimit = new Vector2(-10, 10);

        private Animator _animator;
        private int fishingLayer;
        private Vector2 tiltLimit;

        private CancellationTokenSource layerWeightTokenSource;
        private CancellationTokenSource translateOffsetCts;

        public Transform RodHandle => rightHandle;
        public Transform LeftHandle => leftHandle;
        public AnimatorStateInfo CurrentAnimatorStateInfo => _animator.GetCurrentAnimatorStateInfo(fishingLayer);

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            fishingLayer = _animator.GetLayerIndex("Fishing Layer");
        }

        public void UpdateRotation(FishingCamera camera)
        {
            CastingType castingType = FishingModel.Instance.CastingType.CurrentValue;
            Vector3 targetTilt = castingType == CastingType.Pitching ? pitchingTiltLimit : camera.TiltLimit;
            tiltLimit = Vector3.Lerp(tiltLimit, targetTilt, Time.deltaTime * 5f);

            float currentTilt = Mathf.Clamp(camera.CurrentTilt, tiltLimit.x, tiltLimit.y);
            root.localRotation = Quaternion.Euler(new Vector3(currentTilt, camera.CurrentPan));
        }

        public void PlayAnimation(string name, float weight = 1f)
        {
            _animator.Play(name, fishingLayer);
            _animator.SetLayerWeight(fishingLayer, weight);
        }

        public void PlayIdle(CastingType type)
        {
            _animator.SetInteger("CastingType", (int)type);

            translateOffsetCts?.Cancel();
            translateOffsetCts?.Dispose();
            translateOffsetCts = new CancellationTokenSource();

            offsetValues.TryGetValue(type.ToString(), out var target);
            TranslateOffset(target, 0.2f, translateOffsetCts.Token).Forget();
        }

        async UniTaskVoid TranslateOffset(Transform target, float duration, CancellationToken cancellationToken)
        {
            Vector3 startPosition = offset.localPosition, targetPosition = Vector3.zero;
            Quaternion startRotation = offset.localRotation, targetRotation = Quaternion.identity;
            if (target != null)
            {
                targetPosition = target.localPosition;
                targetRotation = target.localRotation;
            }

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float weight = time / duration;

                offset.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, weight), Quaternion.Lerp(startRotation, targetRotation, weight));
                await UniTask.NextFrame(cancellationToken);
            }

            offset.SetLocalPositionAndRotation(targetPosition, targetRotation);
        }

        public void FadeLayerWeight(float targetWeight, float duration = 0.2f)
        {
            layerWeightTokenSource?.Cancel();
            layerWeightTokenSource?.Dispose();

            layerWeightTokenSource = new CancellationTokenSource();
            UpdateLayerWeight(targetWeight, duration, layerWeightTokenSource.Token).Forget();
        }

        async UniTaskVoid UpdateLayerWeight(float targetWeight, float duration, CancellationToken cancellationToken)
        {
            float startWeight = _animator.GetLayerWeight(fishingLayer);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float weight = Mathf.Lerp(startWeight, targetWeight, elapsed / duration);
                _animator.SetLayerWeight(fishingLayer, weight);
                await UniTask.NextFrame(cancellationToken);
            }
            _animator.SetLayerWeight(fishingLayer, targetWeight);
        }
    }
}