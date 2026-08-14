using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class TransformParentBehaviour : StateMachineBehaviour
    {
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 rotation;
        [SerializeField] float duration;

        private CancellationTokenSource cts;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts = new CancellationTokenSource();
            Translate(animator.transform.parent, position, Quaternion.Euler(rotation), duration, cts.Token).Forget();
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        async UniTaskVoid Translate(Transform target, Vector3 position, Quaternion rotation, float duration, CancellationToken cancellationToken)
        {
            if (target == null)
                return;

            Vector3 startPosition = target.localPosition;
            Quaternion startRotation = target.localRotation;

            float time = 0;
            while (time < duration)
            {
                time += Time.deltaTime;
                float weight = time / duration;

                target.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, position, weight), Quaternion.Lerp(startRotation, rotation, weight));
                await UniTask.NextFrame(cancellationToken);
            }

            target.SetLocalPositionAndRotation(position, rotation);
        }
    }
}