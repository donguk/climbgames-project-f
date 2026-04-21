using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class PhysicsRod : FishingRod
    {
        private CancellationTokenSource cancellationTokenSource;

        void OnDestroy()
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

        public override void CastingLoad()
        {

        }

        public override void OnLoading(float intensity)
        {
            Model.transform.localRotation = Quaternion.AngleAxis(intensity * Mathf.PI * Mathf.Rad2Deg * 0.25f, Vector3.left);
        }

        public override void Swing(float intensity)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

            cancellationTokenSource = new CancellationTokenSource();
            OnSwing(intensity * 30f, cancellationTokenSource.Token).Forget();
        }

        async UniTaskVoid OnSwing(float angle, CancellationToken cancellationToken = default)
        {
            float swingTime = 0.15f;
            float elapsed = 0f;
            Quaternion pullRotation = Model.transform.localRotation;
            Quaternion forwardRotation = Quaternion.Euler(angle, 0, 0); // 앞으로 30도만큼 휘두름

            while (elapsed < swingTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swingTime;
                // EaseOut Quad 공식: 초반엔 빠르고 끝에 살짝 감속
                float easedT = t * (2f - t);

                Model.transform.localRotation = Quaternion.Slerp(pullRotation, forwardRotation, easedT);
                await UniTask.NextFrame(cancellationToken);
            }

            // 원래 위치(Identity)로 부드럽게 복귀
            elapsed = 0f;
            float returnTime = 0.3f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnTime;
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                Model.transform.localRotation = Quaternion.Slerp(forwardRotation, Quaternion.identity, easedT);
                await UniTask.NextFrame(cancellationToken);
            }

            Model.transform.localRotation = Quaternion.identity;
        }
    }
}