using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class LayerWeightBehaviour : StateMachineBehaviour
    {
        [SerializeField] float weight;
        [SerializeField] float duration;

        private CancellationTokenSource cts;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts = new CancellationTokenSource();
            FadeLayerWeight(animator, layerIndex, weight, duration).Forget();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        async UniTaskVoid FadeLayerWeight(Animator animator, int layerIndex, float weight, float duration, CancellationToken cancellationToken = default)
        {
            float startWeight = animator.GetLayerWeight(layerIndex);
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                animator.SetLayerWeight(layerIndex, Mathf.Lerp(startWeight, weight, time / duration));
                await UniTask.NextFrame(cancellationToken);
            }
            animator.SetLayerWeight(layerIndex, weight);
        }
    }
}