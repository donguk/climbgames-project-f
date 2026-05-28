using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FadeLayerWeightBehaviour : StateMachineBehaviour
    {
        [SerializeField] float targetWeight;
        [SerializeField] float durationTime;

        private CancellationTokenSource cts;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts = new CancellationTokenSource();
            FadeLayerWeight(animator, layerIndex, targetWeight, durationTime).Forget();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        async UniTaskVoid FadeLayerWeight(Animator animator, int layerIndex, float targetWeight, float durationTime, CancellationToken cancellationToken = default)
        {
            float startWeight = animator.GetLayerWeight(layerIndex);
            float time = 0f;

            while (time < durationTime)
            {
                time += Time.deltaTime;
                float weight = Mathf.Lerp(startWeight, targetWeight, time / durationTime);
                animator.SetLayerWeight(layerIndex, weight);
                await UniTask.NextFrame(cancellationToken);
            }
            animator.SetLayerWeight(layerIndex, targetWeight);
        }
    }
}