using UnityEngine;
using UnityEngine.UIElements;

namespace ClimbGames.ProjectF
{
    public class TestRotation : MonoBehaviour
    {
        [SerializeField] float period = 1f;
        Quaternion baseRotation;

        void Start()
        {

            baseRotation = transform.rotation;
        }

        void FixedUpdate()
        {
            Debug.Log($"FixedUpdate: {gameObject.name}");

            float time = Mathf.Repeat(Time.time, period);
            float ratio = Mathf.InverseLerp(0f, period, time);

            transform.rotation = baseRotation * Quaternion.AngleAxis(Mathf.Sin(ratio * Mathf.PI) * 180f, Vector3.up);


        }
    }
}