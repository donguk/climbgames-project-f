using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class ColliderTest : MonoBehaviour
    {


        void OnCollisionEnter(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=green>OnCollisionEnter</color>: {collision.gameObject.name}");



        }

        void OnCollisionExit(Collision collision)
        {
            Debug.Log($"[FishingBait] <color=red>OnCollisionExit</color>: {collision.gameObject.name}");

        }
    }
}