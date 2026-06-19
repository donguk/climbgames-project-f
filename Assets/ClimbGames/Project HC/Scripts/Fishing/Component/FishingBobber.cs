using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : FishingEquipment
    {
        [SerializeField] float waterHeight = 1f;
        [SerializeField] AnimationCurve floatingHeight;
        [SerializeField] float loopTime = 1.5f;
        [SerializeField] Transform model;

        public void SetCastingType(CastingType type)
        {
            Joint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            Joint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
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