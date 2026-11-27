using UnityEngine;

namespace ClimbGames.ProjectF
{
    /// <summary>
    /// 낚시대 본 체인을 회전시켜 초릿대 휨을 표현한다.
    /// 팁의 움직임(관성, 떨림, 채비 무게, 입질, 피칭 당김)은 줄 물리(FishingLineBody)가 팁을 스프링에 묶인 질점으로 같이 푼다.
    /// 이 컴포넌트는
    /// - 팁 앵커를 준다: 그립 자세 + 정적 휨(로드 자중, Fighting 게임 텐션 SetLoad)만 반영한 목표 팁 위치 (지금 휜 각도는 쓰지 않음)
    /// - 팁 스프링 속성을 준다
    /// - 줄이 푼 팁 위치에 맞춰 본을 휜다 (메시 팁이 줄 팁과 같은 방향이 되도록 총 휨 각을 역산)
    /// </summary>
    public class FishingRodBender : MonoBehaviour, IRodTip
    {
        private const int TableSize = 128;

        [Header("Bones")]
        [SerializeField] private Transform root;
        [SerializeField] private Transform[] bones;
        [SerializeField] private Transform poleTip;
        [Tooltip("본 인덱스(0=손잡이 쪽, 1=팁 쪽)에 따른 휨 분배")]
        [SerializeField] private AnimationCurve bendCurve = new AnimationCurve(new Keyframe(0f, 0.02f), new Keyframe(0.5f, 0.25f), new Keyframe(1f, 1f));

        [Header("Static Bend")]
        [Tooltip("로드 전체가 휠 수 있는 최대 각도 (deg)")]
        [SerializeField, Range(1f, 120f)] private float maxBendAngle = 90f;
        [Tooltip("Fighting 게임 텐션 1 일 때 휨 각도 (deg)")]
        [SerializeField, Range(0f, 120f)] private float tensionBendAngle = 75f;
        [Tooltip("로드 자체 무게에 의한 처짐 (deg, 로드가 수평일 때). 채비 무게는 줄 물리로 들어감")]
        [SerializeField, Range(0f, 30f)] private float gravityBendAngle = 3f;

        [Header("Tip Spring (줄 물리에서 풂)")]
        [Tooltip("하중 없을 때 팁 고유 진동수 (Hz). 높을수록 빠르고 가볍게 떨림")]
        [SerializeField, Range(0.5f, 8f)] private float tipFrequency = 3.2f;
        [Tooltip("감쇠비. 낮을수록 오래 떨림")]
        [SerializeField, Range(0.02f, 1.5f)] private float dampingRatio = 0.12f;
        [Tooltip("공기 저항 (1/s). 클수록 빠르게 돌리거나 움직일 때 계속 뒤처짐")]
        [SerializeField, Range(0f, 30f)] private float airDrag = 3f;
        [Tooltip("기준 하중 (N). 0.1N ≈ 10g 이 매달린 무게")]
        [SerializeField, Range(0.01f, 5f)] private float referenceTension = 0.1f;
        [Tooltip("기준 하중일 때 휨 각도 (deg). 하중이 커질수록 maxBendAngle 까지 포화")]
        [SerializeField, Range(0.5f, 60f)] private float referenceBendAngle = 12f;

        private Quaternion[] initialRotations;
        private float[] weights;
        private float[] boneOffsets;        // 루트 로컬 Y: 각 본 원점, 마지막은 팁
        private Vector2[] tipTable;         // 총 휨 각 θ(0 ~ maxBendAngle) → 루트 로컬 (횡, 축) 팁 위치
        private float[] polarTable;         // θ → 팁 위치각 (deg)
        private bool isCached;

        private Vector3 loadDirection;
        private float loadTension;

        private Vector2 bend;               // 루트 로컬 (x, z) 평면의 총 휨 벡터 (deg)

        public Vector2 CurrentBend => bend;
        public float CurrentBendAngle => bend.magnitude;
        public Transform PoleTip => poleTip;

        void Awake()
        {
            CacheBones();
        }

        void OnDisable()
        {
            ResetBend();
        }

        private void CacheBones()
        {
            isCached = false;
            if (root == null || bones == null || bones.Length == 0)
                return;

            initialRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; ++i)
                initialRotations[i] = bones[i] != null ? bones[i].localRotation : Quaternion.identity;

            // 쉬는 자세에서 본은 루트 로컬 Y 축 위에 일렬
            boneOffsets = new float[bones.Length + 1];
            for (int i = 0; i < bones.Length; ++i)
                boneOffsets[i] = bones[i] != null ? root.InverseTransformPoint(bones[i].position).y : 0f;

            Transform last = bones[bones.Length - 1];
            Transform tip = poleTip != null ? poleTip : (last.childCount > 0 ? last.GetChild(0) : last);
            boneOffsets[bones.Length] = root.InverseTransformPoint(tip.position).y;

            CacheWeights();
            isCached = true;
            BuildTables();
        }

        // 본 정보가 준비됐는지: 아직이면 지금 준비를 시도 (Awake 전에 불리거나 에디터에서 Awake 없이 쓸 때), root·bones 가 없으면 false
        private bool EnsureCached()
        {
            if (isCached == false)
                CacheBones();
            return isCached;
        }

        private void CacheWeights()
        {
            weights = new float[bones.Length];

            float sum = 0f;
            for (int i = 0; i < bones.Length; ++i)
            {
                float t = bones.Length > 1 ? (float)i / (bones.Length - 1) : 1f;
                weights[i] = Mathf.Max(0f, bendCurve.Evaluate(t));
                sum += weights[i];
            }

            for (int i = 0; i < weights.Length; ++i)
                weights[i] = sum > 0f ? weights[i] / sum : 1f / weights.Length;
        }

        // 총 휨 각 θ 일 때 루트 로컬 (횡, 축) 팁 위치 (본 i 가 θ·w_i 만큼 같은 방향으로 꺾인 평면 체인)
        private Vector2 EvaluateTip(float bendDegrees)
        {
            float x = 0f, y = boneOffsets[0];
            float angle = 0f;
            for (int i = 0; i < bones.Length; ++i)
            {
                angle += bendDegrees * weights[i] * Mathf.Deg2Rad;
                float length = boneOffsets[i + 1] - boneOffsets[i];
                x += Mathf.Sin(angle) * length;
                y += Mathf.Cos(angle) * length;
            }
            return new Vector2(x, y);
        }

        private void BuildTables()
        {
            tipTable = new Vector2[TableSize];
            polarTable = new float[TableSize];
            for (int i = 0; i < TableSize; ++i)
            {
                float theta = maxBendAngle * i / (TableSize - 1);
                tipTable[i] = EvaluateTip(theta);
                polarTable[i] = Mathf.Atan2(tipTable[i].x, tipTable[i].y) * Mathf.Rad2Deg;
            }
        }

        private Vector2 SampleTip(float bendDegrees)
        {
            float f = Mathf.Clamp(bendDegrees / maxBendAngle, 0f, 1f) * (TableSize - 1);
            int i = Mathf.Min((int)f, TableSize - 2);
            return Vector2.Lerp(tipTable[i], tipTable[i + 1], f - i);
        }

        // 팁 위치각 → 총 휨 각 (위치각은 θ 에 대해 단조 증가)
        private float PolarToBend(float polarDegrees)
        {
            if (polarDegrees <= 0f)
                return 0f;
            if (polarDegrees >= polarTable[TableSize - 1])
                return maxBendAngle;

            int lo = 0, hi = TableSize - 1;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (polarTable[mid] < polarDegrees)
                    lo = mid;
                else
                    hi = mid;
            }

            float range = polarTable[hi] - polarTable[lo];
            float t = range > 1e-6f ? (polarDegrees - polarTable[lo]) / range : 0f;
            return maxBendAngle * (lo + t) / (TableSize - 1);
        }

        /// <summary>Fighting 게임 텐션: 줄이 당기는 방향(월드)과 텐션(0~1)</summary>
        public void SetLoad(Vector3 worldDirection, float tension01)
        {
            loadDirection = worldDirection;
            loadTension = Mathf.Clamp01(float.IsNaN(tension01) ? 0f : tension01);
        }

        public void ClearLoad()
        {
            loadDirection = Vector3.zero;
            loadTension = 0f;
        }

        public void ResetBend()
        {
            ClearLoad();
            bend = Vector2.zero;
            ApplyBones();
        }

        // IRodTip ---------------------------------------------------------------

        // ① 정적 휨 각도: 로드 자중 + Fighting 게임 텐션 (그립 로컬 x·z, deg) — 줄·관성으로 지금 휜 각도는 쓰지 않음
        // ② BendToLocalTip: 본 체인 표(tipTable)에서 그 각도일 때 그립 로컬 팁 위치 (0 이면 곧은 로드 끝)
        // ③ root.TransformPoint: 그립의 지금 위치·회전으로 월드 변환
        public Vector3 GetTipAnchor()
        {
            if (!EnsureCached())
                return poleTip != null ? poleTip.position : transform.position;

            Vector2 staticBend = Vector2.ClampMagnitude(GetLoadBend() + GetGravityBend(), maxBendAngle);
            return root.TransformPoint(BendToLocalTip(staticBend));
        }

        public FishingLineBody.TipSpring GetTipSpring()
        {
            if (!EnsureCached())
                return default;

            Vector2 rest = tipTable[0];
            return new FishingLineBody.TipSpring()
            {
                enabled = true,
                frequency = tipFrequency,
                dampingRatio = dampingRatio,
                airDrag = airDrag,
                referenceForce = referenceTension,
                referenceDistance = Vector2.Distance(SampleTip(Mathf.Min(referenceBendAngle, maxBendAngle * 0.95f)), rest),
                maxDistance = Vector2.Distance(SampleTip(maxBendAngle), rest),
                pivot = root.position,
            };
        }

        public Vector3 ApplyTip(Vector3 simulatedTip)
        {
            if (!EnsureCached())
                return simulatedTip;

            Vector3 local = root.InverseTransformPoint(simulatedTip);
            Vector2 lateral = new Vector2(local.x, local.z);
            float lateralLength = lateral.magnitude;

            if (lateralLength > 1e-6f && float.IsNaN(lateralLength) == false)
            {
                float polar = Mathf.Atan2(lateralLength, local.y) * Mathf.Rad2Deg;
                bend = lateral / lateralLength * PolarToBend(polar);
            }
            else
            {
                bend = Vector2.zero;
            }

            ApplyBones();
            return poleTip != null ? poleTip.position : simulatedTip;
        }

        // ----------------------------------------------------------------------

        private Vector3 BendToLocalTip(Vector2 bendDegrees)
        {
            float angle = bendDegrees.magnitude;
            Vector2 tip = SampleTip(angle);
            if (angle < 1e-4f)
                return new Vector3(0f, tip.y, 0f);

            Vector2 direction = bendDegrees / angle;
            return new Vector3(direction.x * tip.x, tip.y, direction.y * tip.x);
        }

        private Vector2 GetLoadBend()
        {
            if (loadTension <= 0f || loadDirection.sqrMagnitude < 1e-6f)
                return Vector2.zero;

            Vector3 local = root.InverseTransformDirection(loadDirection);
            Vector2 perpendicular = new Vector2(local.x, local.z);
            if (perpendicular.sqrMagnitude < 1e-6f)
                return Vector2.zero;

            return perpendicular.normalized * (loadTension * tensionBendAngle);
        }

        private Vector2 GetGravityBend()
        {
            Vector3 local = root.InverseTransformDirection(Vector3.down);
            return new Vector2(local.x, local.z) * gravityBendAngle;
        }

        private void ApplyBones()
        {
            if (isCached == false || initialRotations == null || initialRotations.Length != bones.Length)
                return;

            float angle = bend.magnitude;
            Vector3 axis = Vector3.right;
            if (angle > 1e-4f)
            {
                // 루트 로컬 Y(로드 축)를 휨 방향(x, z)으로 기울이는 회전축
                Vector3 direction = new Vector3(bend.x, 0f, bend.y) / angle;
                axis = Vector3.Cross(Vector3.up, direction);
            }

            for (int i = 0; i < bones.Length; ++i)
            {
                if (bones[i] == null)
                    continue;

                bones[i].localRotation = Quaternion.AngleAxis(angle * weights[i], axis) * initialRotations[i];
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // 커브·최대 각 변경 시 가중치와 테이블만 다시 계산 (초기 회전은 유지)
            if (Application.isPlaying && isCached)
            {
                CacheWeights();
                BuildTables();
            }
        }
#endif
    }
}
