using UnityEngine;

namespace ClimbGames.Project_HC
{
    public abstract class FishingRod : MonoBehaviour
    {
        [SerializeField] private Transform poleTip;

        public Transform PoleTipe => poleTip;

        public virtual void OnLoading(FishingInput.ICastAction castAction)
        {

        }
    }
}