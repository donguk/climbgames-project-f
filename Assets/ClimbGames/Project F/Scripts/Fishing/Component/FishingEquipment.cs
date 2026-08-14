using UnityEngine;
using R3;

namespace ClimbGames.ProjectF
{
    public class FishingEquipment : MonoBehaviour
    {
        private int id;
        private MessageBroker messageBroker;
        private CompositeDisposable disposables = new CompositeDisposable();

        protected MessageBroker MessageBroker => messageBroker;

        public void Initialize(int id)
        {
            this.id = id;

            disposables.Clear();
            messageBroker = FishingEvent.GetBroker(id);

            messageBroker.Receive<FishingEvent.OnCastingTypeChanged>().Subscribe(_ => OnCastingTypeChanged(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnCastingThrow>().Subscribe(_ => OnCastingThrow(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnBobberEnterWater>().Subscribe(_ => OnBobberEnterWater(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnFishBite>().Subscribe(_ => OnFishBite(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnFishFlee>().Subscribe(_ => OnFishFlee(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnFishSubmit>().Subscribe(_ => OnFishSubmit(_)).AddTo(this);
            messageBroker.Receive<FishingEvent.OnPullOut>().Subscribe(_ => OnPullOut(_)).AddTo(this);
        }

        protected virtual void OnCastingTypeChanged(FishingEvent.OnCastingTypeChanged param)
        {
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

        protected virtual void OnPullOut(FishingEvent.OnPullOut param)
        {
        }
    }
}