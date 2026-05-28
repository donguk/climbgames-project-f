using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingCharacter : MonoBehaviour
    {
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform rightHandle;
        [SerializeField] private Transform leftHandle;

        private Animator _animator;
        private int fishingLayer;
        private CancellationTokenSource layerWeightTokenSource;

        public Transform RodHandle => rightHandle;
        public Transform LeftHandle => leftHandle;
        public Transform ModelRoot => modelRoot;
        public AnimatorStateInfo CurrentAnimatorStateInfo => _animator.GetCurrentAnimatorStateInfo(fishingLayer);

        void Awake()
        {
            _animator = GetComponentInChildren<Animator>();
            fishingLayer = _animator.GetLayerIndex("Fishing Layer");
        }

        public void UpdateRotation(FishingCameraController controller)
        {
            modelRoot.rotation = controller.Pivot.rotation;
        }

        public void PlayAnimation(string name, float weight = 1f)
        {
            _animator.Play(name, fishingLayer);
            _animator.SetLayerWeight(fishingLayer, weight);
        }

        public void PlayIdle(CastingType type)
        {
            _animator.SetInteger("CastingType", (int)type);
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