using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private ConfigurableJoint configurableJoint;

        [SerializeField] float waterHeight = 1f;
        [SerializeField] AnimationCurve floatingHeight;
        [SerializeField] float loopTime = 1.5f;
        [SerializeField] Transform model;

        void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();

            if (configurableJoint == null)
                configurableJoint = GetComponent<ConfigurableJoint>();
        }

        public void Attach(Transform lineBobber)
        {
            transform.position = lineBobber.position;
            configurableJoint.connectedBody = lineBobber.GetComponent<Rigidbody>();
        }

        public void Stop()
        {
            transform.localRotation = Quaternion.identity;
            model.localPosition = Vector3.zero;
            enabled = false;
        }

        public void Play()
        {
            enabled = true;
        }

        //void Update()
        //{
        //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime);
        //
        //    if (model == null)
        //        return;
        //
        //    float t = Mathf.Repeat(Time.time, loopTime) / loopTime;
        //    Vector3 position = model.localPosition;
        //    position.y = waterHeight * floatingHeight.Evaluate(t);
        //    model.localPosition = position;
        //}
    }
}