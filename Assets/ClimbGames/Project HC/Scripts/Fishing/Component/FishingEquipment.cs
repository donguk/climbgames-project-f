using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingEquipment : MonoBehaviour
    {
        private CompositeDisposable disposables = new CompositeDisposable();

        void OnDestroy()
        {
            disposables.Dispose();
            disposables = null;
        }

        void OnEnable()
        {
            disposables.Clear();

            FishingEvent.Broker.Receive<FishingEvent.OnCastingThrow>().Subscribe(_ => OnCastingThrow(_)).AddTo(this);
            FishingEvent.Broker.Receive<FishingEvent.OnBobberEnterWater>().Subscribe(_ => OnBobberEnterWater(_)).AddTo(this);
            FishingEvent.Broker.Receive<FishingEvent.OnFishBite>().Subscribe(_ => OnFishBite(_)).AddTo(this);
            FishingEvent.Broker.Receive<FishingEvent.OnFishFlee>().Subscribe(_ => OnFishFlee(_)).AddTo(this);
            FishingEvent.Broker.Receive<FishingEvent.OnFishSubmit>().Subscribe(_ => OnFishSubmit(_)).AddTo(this);
        }

        void OnDisable()
        {
            disposables.Clear();
        }

        protected virtual void OnCastingThrow(FishingEvent.OnCastingThrow param)
        {
        }

        protected virtual void OnBobberEnterWater(FishingEvent.OnBobberEnterWater param)
        {
        }

        protected virtual void OnFishBite(FishingEvent.OnFishBite param)
        {
        }

        protected virtual void OnFishFlee(FishingEvent.OnFishFlee param)
        {
        }

        protected virtual void OnFishSubmit(FishingEvent.OnFishSubmit param)
        {
        }
    }
}