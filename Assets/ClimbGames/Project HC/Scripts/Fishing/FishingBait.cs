using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingBait : MonoBehaviour
    {
        [SerializeField] private Rigidbody modelBody;
        [SerializeField] private ConfigurableJoint configurableJoint;

        void Awake()
        {
            if (modelBody == null)
                modelBody = GetComponent<Rigidbody>();

            if (configurableJoint == null)
                configurableJoint = GetComponent<ConfigurableJoint>();
        }

        public void Setup(FishingRod fishingRod)
        {
            // 미끼 위치를 초릿대 바로 아래로 강제 이동
            float lineLength = 0.5f; // 원하는 낚시줄 길이
            transform.position = fishingRod.PoleTipe.position + (Vector3.down * lineLength);

            modelBody.linearVelocity = Vector3.zero;
            modelBody.angularVelocity = Vector3.zero;

            configurableJoint.connectedBody = fishingRod.PoleTipe.GetComponent<Rigidbody>();
        }
    }
}