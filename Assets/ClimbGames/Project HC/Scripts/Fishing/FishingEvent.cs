using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public interface IFishingEvent
    {
        // 타입 구분이 필요한 경우
    }

    public static class FishingEvent
    {
        private static Dictionary<int, MessageBroker> _brokers = new Dictionary<int, MessageBroker>();
        public static MessageBroker GetBroker(int id)
        {
            if (_brokers.TryGetValue(id, out var broker) == false)
                _brokers[id] = broker = new MessageBroker();

            return broker;
        }


        // 이벤트

        public struct OnCastingTypeChanged : IFishingEvent
        {
            public CastingType type;
            public Transform leftHandle;
        }

        public struct OnCastingStarted : IFishingEvent
        {
            public float intensity;
        }

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

        public struct OnPullOut : IFishingEvent
        {
            public HookedFish hookedFish;
            public FishingRod rod;
        }

        public struct OnFishCatched : IFishingEvent
        {
            public FishData fishData;
        }
    }
}