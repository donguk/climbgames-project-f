using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    [ExecuteAlways]
    public class FishingRod : MonoBehaviour
    {
        [SerializeField] private GameObject modelRoot;
        [SerializeField] private GameObject rodModel;
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform lure;
        [SerializeField] private LineRenderer lineRenderer;

        private bool isCasting;
        private Vector2 startCastingPosition;
        private float castingPower = 0f;

        void Start()
        {
            lineRenderer.positionCount = 2;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (isCasting)
                {
                    var position = mouse.position;
                    float dist = Vector2.Distance(position.value, startCastingPosition);
                    if (dist > 0f)
                    {
                        castingPower = Mathf.Clamp01(dist / Screen.height);
                        rodModel.transform.localRotation = Quaternion.AngleAxis(castingPower * Mathf.PI * Mathf.Rad2Deg * 0.25f, Vector3.left);
                    }
                }

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    var position = mouse.position;
                    startCastingPosition = position.value;
                    if (startCastingPosition.x > 0 && startCastingPosition.x < Screen.width && startCastingPosition.y > 0 && startCastingPosition.y < Screen.height)
                        isCasting = true;
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    if (isCasting)
                    {
                        isCasting = false;
                        ExecuteCasting(castingPower);
                    }
                }
            }
        }

        void LateUpdate()
        {
            if (rodTip == null || lure == null) return;

            // 1. Line의 시작점을 낚싯대 끝으로 업데이트
            lineRenderer.SetPosition(0, rodTip.position);

            // 2. Line의 끝점을 미끼 위치로 업데이트
            lineRenderer.SetPosition(1, lure.position);
        }

        public void Setup(FishingPoint fishingPoint)
        {
            modelRoot.transform.SetPositionAndRotation(fishingPoint.RodRoot.position, fishingPoint.RodRoot.rotation);
            modelRoot.transform.localScale = fishingPoint.RodRoot.localScale;

            // 2. 미끼 위치를 초릿대 바로 아래로 강제 이동
            float lineLength = 1f; // 원하는 낚시줄 길이
            lure.transform.position = rodTip.position + (Vector3.down * lineLength);

            // 3. 미끼의 속도 초기화 (혹시 모를 가속도 제거)
            var rb = lure.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        async void ExecuteCasting(float power, CancellationToken ct = default)
        {
            // 1. 앞으로 강하게 휘두르기 (Overshoot)
            float swingTime = 0.15f;
            float elapsed = 0f;
            Quaternion pullRotation = rodModel.transform.localRotation;
            Quaternion forwardRotation = Quaternion.Euler(30f * power, 0, 0); // 앞으로 30도만큼 휘두름

            while (elapsed < swingTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swingTime;
                // EaseOut Quad 공식: 초반엔 빠르고 끝에 살짝 감속
                float easedT = t * (2f - t);

                rodModel.transform.localRotation = Quaternion.Slerp(pullRotation, forwardRotation, easedT);
                await UniTask.NextFrame(ct);
            }

            // 2. 원래 위치(Identity)로 부드럽게 복귀
            elapsed = 0f;
            float returnTime = 0.3f;
            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / returnTime;
                float easedT = Mathf.SmoothStep(0f, 1f, t);

                rodModel.transform.localRotation = Quaternion.Slerp(forwardRotation, Quaternion.identity, easedT);
                await UniTask.NextFrame(ct);
            }

            rodModel.transform.localRotation = Quaternion.identity;
            Debug.Log($"Casting Complete with Power: {power}");
        }
    }
}