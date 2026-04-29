using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class VerletLineDemo : MonoBehaviour
    {
        [SerializeField] private VerletLine verletLine;
        [SerializeField] private Transform startPoint;

        [SerializeField] private VerletLine fishingLine;
        [SerializeField] private FishingRod fishingRod;

        void Start()
        {
            //verletLine.Setup(startPoint);

            //fishingLine.Setup(fishingRod.PoleTip);
        }
    }
}