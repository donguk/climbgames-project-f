using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingBobber : FishingEquipment
    {
        [SerializeField] AnimationCurve floatingHeight;
        [SerializeField] Transform modelRoot;

        private float waterHeight;

        public static bool isFloating;
        public static Vector3 floatingPosition;

        //void Update()
        //{
        //    if (Joint != null && Joint.connectedBody != null)
        //    {
        //        //
        //    }
        //    else if (Point != null)
        //    {
        //        Vector3 position = Point.transform.position;
        //        if (position.y < waterHeight)
        //        {
        //            position.y = waterHeight;
        //            transform.position = position;
        //
        //            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime);
        //        }
        //    }
        //}

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[FishingBobber] OnTggerEnter: {other.gameObject.name}");
            if (FishingLayer.Water == other.gameObject.layer)
            {
                Vector3 hitPoint = other.ClosestPoint(transform.position);
                //waterHeight = position.y;
                //
                //Joint.connectedBody = null;
                //Body.isKinematic = true;

                // 테스트
                isFloating = true;
                floatingPosition = hitPoint;
            }
        }

        public void SetCastingType(CastingType type)
        {
            Joint.angularXMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            Joint.angularZMotion = type == CastingType.Pitching ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
        }
    }
}