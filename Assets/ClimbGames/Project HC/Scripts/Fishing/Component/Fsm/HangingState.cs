using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLineController
    {
        public class HangingState : FishingLineStateBase
        {
            public HangingState(FishingLine manager, FishingLineController controller) : base(manager, controller)
            {
            }

            protected override void OnEnter()
            {
                //Handle.Body.linearVelocity = Vector3.zero;
                //Handle.Body.angularVelocity = Vector3.zero;
                //Handle.Body.linearDamping = 1f;
                //Handle.Body.excludeLayers |= 1 << FishingLayer.Water | 1 << FishingLayer.Ground;
            }
        }
    }
}