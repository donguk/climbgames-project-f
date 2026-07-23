using System;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public interface IFishingEvent
    {
        // 타입 구분이 필요한 경우 사용
    }


    // !다중 캐릭터 일 경우 broker 분리 필요!
    public static class FishingEvent
    {
        private static readonly Lazy<MessageBroker> _broker = new(() => new MessageBroker());
        public static MessageBroker Broker => _broker.Value;


        public struct OnCastingThrow : IFishingEvent
        {
            public Vector3 velocity;
        }

        public struct OnHookGrounded : IFishingEvent
        {
        }

        public struct OnBobberEnterWater : IFishingEvent
        {
        }

        public struct OnBobberLandedWater : IFishingEvent
        {
        }

        public struct OnFishBite : IFishingEvent
        {
            public HookedFish hookedFish;
        }

        public struct OnFishFlee : IFishingEvent
        {
        }

        public struct OnFishSubmit : IFishingEvent
        {
            public HookedFish hookedFish;
        }
    }
}