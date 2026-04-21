using System.Collections;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class BaitCasting : MonoBehaviour
    {
        [Header("References")]
        public Rigidbody baitRigidbody;
        public ConfigurableJoint baitJoint;
        public Transform poleTip;
        public LineRenderer trajectoryLine;

        [Header("Cast Settings")]
        public float minCastSpeed = 5f;
        public float maxCastSpeed = 20f;
        public float loadIntensity;

        [Header("Landing Zone")]
        public Vector3 zoneCenter;       // 유효 착수 구역 중심
        public float zoneRadius = 3f;    // 유효 착수 반경
        public LayerMask waterLayer;     // 물 레이어

        [Header("Correction")]
        [Range(0f, 1f)]
        public float correctionStrength = 0.6f; // 0=보정없음, 1=강제
        public float correctionStartTime = 0.4f; // 착수 N초 전부터 보정 시작

        private Vector3 prevTipPos;
        private bool isFired = false;
        private bool isCorreecting = false;
        private Vector3 correctionTarget;

        void Update()
        {
            if (!isFired)
            {
                Vector3 tipVelocity = (poleTip.position - prevTipPos) / Time.deltaTime;
                DrawTrajectory(poleTip.position, GetFireVelocity(tipVelocity));
            }
            else if (!isCorreecting)
            {
                CheckAndCorrect();
            }

            prevTipPos = poleTip.position;
        }

        // ───────────────────────────────
        // Fire
        // ───────────────────────────────
        public void OnFireInput()
        {
            if (isFired) return;

            Vector3 tipVelocity = (poleTip.position - prevTipPos) / Time.deltaTime;
            Fire(tipVelocity);
        }

        void Fire(Vector3 tipVelocity)
        {
            isFired = true;

            baitJoint.connectedBody = null;
            baitRigidbody.isKinematic = false;
            baitRigidbody.useGravity = true;
            baitRigidbody.linearVelocity = GetFireVelocity(tipVelocity);
            baitRigidbody.angularVelocity = new Vector3(Random.Range(-5f, 5f), 0, 10f);

            trajectoryLine.enabled = false;
        }

        Vector3 GetFireVelocity(Vector3 tipVelocity)
        {
            float speed = Mathf.Lerp(minCastSpeed, maxCastSpeed, loadIntensity);
            Vector3 fireDir = tipVelocity.normalized;

            if (fireDir.y < 0.1f)
            {
                fireDir.y = 0.1f;
                fireDir = fireDir.normalized;
            }

            return fireDir * speed;
        }

        // ───────────────────────────────
        // 착수 예측 & 보정
        // ───────────────────────────────
        void CheckAndCorrect()
        {
            // 현재 속도 기준으로 착수 예상 지점 계산
            Vector3 predictedLanding = PredictLanding(
                baitRigidbody.position,
                baitRigidbody.linearVelocity);

            // 유효 구역 안이면 보정 불필요
            Vector2 flat = new Vector2(
                predictedLanding.x - zoneCenter.x,
                predictedLanding.z - zoneCenter.z);

            if (flat.magnitude <= zoneRadius) return;

            // 구역 밖이면 → 경계 위 가장 가까운 지점으로 보정 목표 설정
            Vector2 clamped = flat.normalized * zoneRadius;
            correctionTarget = new Vector3(
                zoneCenter.x + clamped.x,
                zoneCenter.y,           // 수면 높이
                zoneCenter.z + clamped.y);

            // 착수까지 남은 시간 계산
            float timeToLand = TimeToLand(
                baitRigidbody.position,
                baitRigidbody.linearVelocity);

            // 보정 시작 시간 안이면 즉시 보정
            if (timeToLand <= correctionStartTime)
            {
                StartCoroutine(SmoothCorrect(timeToLand));
                isCorreecting = true;
            }
        }

        IEnumerator SmoothCorrect(float duration)
        {
            float elapsed = 0f;
            Vector3 originalVelocity = baitRigidbody.linearVelocity;

            // 보정 목표까지의 필요 초기 속도 역산
            Vector3 requiredVelocity = CalculateLaunchVelocity(
                baitRigidbody.position,
                correctionTarget,
                duration);

            while (elapsed < duration * 0.8f) // 착수 직전 80% 구간에서 보정
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (duration * 0.8f);

                // 부드럽게 보간 (SmoothStep으로 자연스럽게)
                float blend = Mathf.SmoothStep(0f, correctionStrength, t);
                baitRigidbody.linearVelocity = Vector3.Lerp(
                    originalVelocity,
                    requiredVelocity,
                    blend);

                yield return null;
            }
        }

        // ───────────────────────────────
        // 물리 유틸
        // ───────────────────────────────

        // 현재 위치+속도에서 착수 예상 지점 시뮬
        Vector3 PredictLanding(Vector3 pos, Vector3 vel)
        {
            float t = 0f;
            float step = 0.05f;
            Vector3 cur = pos;
            Vector3 v = vel;

            for (int i = 0; i < 200; i++)
            {
                Vector3 next = cur + v * step + 0.5f * Physics.gravity * step * step;
                v += Physics.gravity * step;

                // 물 레이어 또는 y=waterY 이하로 내려오면 착수
                if (Physics.Linecast(cur, next, waterLayer) ||
                    next.y <= zoneCenter.y)
                {
                    return next;
                }

                cur = next;
                t += step;
            }

            return cur;
        }

        float TimeToLand(Vector3 pos, Vector3 vel)
        {
            float t = 0f;
            float step = 0.05f;
            Vector3 cur = pos;
            Vector3 v = vel;

            for (int i = 0; i < 200; i++)
            {
                Vector3 next = cur + v * step + 0.5f * Physics.gravity * step * step;
                v += Physics.gravity * step;

                if (Physics.Linecast(cur, next, waterLayer) ||
                    next.y <= zoneCenter.y)
                {
                    return t;
                }

                cur = next;
                t += step;
            }

            return t;
        }

        // 목표 지점까지 duration 안에 도달하는 초기 속도 역산
        Vector3 CalculateLaunchVelocity(Vector3 from, Vector3 to, float duration)
        {
            float g = Mathf.Abs(Physics.gravity.y);
            Vector3 delta = to - from;

            float vx = delta.x / duration;
            float vz = delta.z / duration;
            float vy = (delta.y + 0.5f * g * duration * duration) / duration;

            return new Vector3(vx, vy, vz);
        }

        // ───────────────────────────────
        // 예측선
        // ───────────────────────────────
        void DrawTrajectory(Vector3 startPos, Vector3 initVelocity)
        {
            trajectoryLine.enabled = true;

            int steps = 40;
            float timeStep = 0.05f;
            trajectoryLine.positionCount = steps;

            for (int i = 0; i < steps; i++)
            {
                float t = i * timeStep;
                Vector3 pos = startPos
                    + initVelocity * t
                    + 0.5f * Physics.gravity * t * t;
                trajectoryLine.SetPosition(i, pos);
            }
        }
    }
}