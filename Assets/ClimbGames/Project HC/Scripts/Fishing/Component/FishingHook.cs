using System;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingHook : FishingEquipment
    {
        private IDisposable disposable;

        public void UpdatePosition()
        {
            if (Joint != null && Joint.connectedBody != null)
            {

            }
            else if (Point != null)
            {
                transform.position = Point.transform.position;
                transform.up = Point.Up;
            }
        }

        public override void Attach(FishingLinePoint point)
        {
            base.Attach(point);

            //
            disposable?.Dispose();
            disposable = point.OnKinematicChangedAsObservable().Subscribe(_ =>
            {
                if (_ == false)
                {
                    Joint.connectedBody = null;
                    Body.isKinematic = true;
                }
            }).AddTo(this);
        }
    }
}