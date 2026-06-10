using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : LineAdaptor
    {
        [SerializeField] float waterHeight = 1f;
        [SerializeField] AnimationCurve floatingHeight;
        [SerializeField] float loopTime = 1.5f;
        [SerializeField] Transform model;

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