using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClimbGames.Project_HC
{
    [ExecuteAlways]
    public class FishingRod_RND : MonoBehaviour
    {
        [SerializeField] private GameObject modelRoot;
        [SerializeField] private GameObject rodModel;
        [SerializeField] private Transform rodTip;
        [SerializeField] private Transform lure;
        [SerializeField] private LineRenderer lineRenderer;

        private bool isCasting;
        private Vector2 startCastingPosition;
        private float castingPower = 0f;
        private FishingPoint fishingPoint;
        private Rigidbody rodRigidbody, lureRigidbody;

        void Start()
        {
            lineRenderer.positionCount = 2;

            rodRigidbody = rodTip.GetComponent<Rigidbody>();
            lureRigidbody = lure.GetComponent<Rigidbody>();
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

        Vector3 poleTipVelocity;
        Vector3 lastPoleTipPosition;
        float lastForwardMomenTum, forwardMomentum;
        void FixedUpdate()
        {
            // 1. 초릿대의 현재 위치에서 지난 위치를 빼서 속도 계산
            poleTipVelocity = (rodTip.position - lastPoleTipPosition) / Time.fixedDeltaTime;
            lastPoleTipPosition = rodTip.position;

            if (isOverShoot)
            {
                Vector3 relativeVelocity = lureRigidbody.linearVelocity - poleTipVelocity;
                Vector3 forwardDir = rodModel.transform.forward;
                // 미끼가 앞으로 튀어나가려는 힘(내적) 확인
                forwardMomentum = Vector3.Dot(relativeVelocity, forwardDir);
                Debug.Log($"poleTipVelocity({poleTipVelocity})/ lureRigidbody.linearVelocity({lureRigidbody.linearVelocity})/ relativeVelocity({relativeVelocity})/ forwardMomentum({forwardMomentum})");
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
            this.fishingPoint = fishingPoint;

            modelRoot.transform.SetPositionAndRotation(fishingPoint.CharacterPoint.position, fishingPoint.CharacterPoint.rotation);
            modelRoot.transform.localScale = fishingPoint.CharacterPoint.localScale;

            // 2. 미끼 위치를 초릿대 바로 아래로 강제 이동
            float lineLength = 1.5f; // 원하는 낚시줄 길이
            lure.transform.position = rodTip.position + (Vector3.down * lineLength);

            // 3. 미끼의 속도 초기화 (혹시 모를 가속도 제거)
            var rb = lure.GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        bool isCastLure;
        bool isOverShoot;
        async void ExecuteCasting(float power, CancellationToken ct = default)
        {
            // 1. 앞으로 강하게 휘두르기 (Overshoot)
            float swingTime = 0.15f;
            float elapsed = 0f;
            Quaternion pullRotation = rodModel.transform.localRotation;
            Quaternion forwardRotation = Quaternion.Euler(30f * power, 0, 0); // 앞으로 30도만큼 휘두름

            Vector3 relativeVelocity = lureRigidbody.linearVelocity - poleTipVelocity;
            lastForwardMomenTum = -Mathf.Infinity;
            isCastLure = false;
            isOverShoot = true;
            while (elapsed < swingTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / swingTime;
                // EaseOut Quad 공식: 초반엔 빠르고 끝에 살짝 감속
                float easedT = t * (2f - t);

                rodModel.transform.localRotation = Quaternion.Slerp(pullRotation, forwardRotation, easedT);
                await UniTask.NextFrame(ct);

                // 케스팅 시점 체크
                // 1.
                {
                    if (lastForwardMomenTum > 0f && forwardMomentum < lastForwardMomenTum && isCastLure == false)
                    {
                        // XZ 평면의 크기와 Y축 높이 사이의 각도를 구합니다.
                        float xzLen = new Vector3(relativeVelocity.x, 0, relativeVelocity.z).magnitude;
                        float finalAngle = Mathf.Atan2(relativeVelocity.y, xzLen) * Mathf.Rad2Deg;

                        // 수평 이하(0도 미만)로 날아가는 것을 방지
                        finalAngle = Mathf.Clamp(finalAngle, 15f, 60f);

                        Debug.Log($"lastForwardMomenTum({lastForwardMomenTum})/ forwardMomentum({forwardMomentum})");
                        SetLureVelocity(lure.position, fishingPoint.CastPosition, finalAngle);
                        isCastLure = true;
                    }
                    lastForwardMomenTum = forwardMomentum;
                }
            }
            isOverShoot = false;

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
        }

        public void SetLureVelocity(Vector3 start, Vector3 target, float angle)
        {
            var baitRigidbody = AssetManager.Instantiate<Rigidbody>("Test_Bait");
            baitRigidbody.transform.position = lure.position;

            Vector3 direction = target - start;
            float h = direction.y; // 높이 차 (yOffset)
            direction.y = 0; // 수평 거리 계산을 위해 y축 제거
            float d = direction.magnitude; // 수평 거리 (distance)
            float a = angle * Mathf.Deg2Rad; // 라디안 변환
            float g = Physics.gravity.magnitude;

            // 포물선 속도 공식 (분모 0 체크 포함)
            float cosA = Mathf.Cos(a);
            float v2 = (g * d * d) / (2 * cosA * cosA * (d * Mathf.Tan(a) - h));

            if (v2 <= 0)
            {
                Debug.LogWarning("해당 각도로는 목표 지점에 도달할 수 없습니다.");
                return;
            }

            float v = Mathf.Sqrt(v2);

            // 발사 벡터 생성
            // 1. 수평 방향으로 정규화된 벡터
            Vector3 launchVec = direction.normalized;

            // 2. 수평 벡터를 위쪽(Y)으로 '각도'만큼 회전
            // 수평 성분은 cos(a), 수직 성분은 sin(a)의 비율을 가집니다.
            Vector3 finalVelocity = launchVec * cosA;
            finalVelocity.y = Mathf.Sin(a);

            // 3. 최종 속도 크기 곱하기
            baitRigidbody.linearVelocity = finalVelocity * v;
        }
    }
}