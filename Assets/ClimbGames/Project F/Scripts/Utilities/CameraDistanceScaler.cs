using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class CameraDistanceScaler : MonoBehaviour
    {
        [SerializeField] private float minDistance;
        [SerializeField] private float maxDistance;
        [SerializeField] private float maxScale = 8f;

        private Vector3 originScale = Vector3.one;

        protected void Awake()
        {
            originScale = transform.localScale;
        }

        void Update()
        {
            float dist = Vector3.Distance(transform.position, Framework.MainCamera.transform.position);
            float t = Mathf.InverseLerp(minDistance, maxDistance, dist);
            transform.localScale = Vector3.Lerp(originScale, new Vector3(maxScale, maxScale, maxScale), t);
        }
    }
}