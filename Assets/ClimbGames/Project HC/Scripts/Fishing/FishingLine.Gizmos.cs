using UnityEngine;

namespace ClimbGames.Project_HC
{
    public partial class FishingLine
    {
        void DrawSphere(Transform transform, Color color = default)
        {
            if (transform != null && transform.gameObject.activeSelf)
            {
                Color origin = Gizmos.color;
                Gizmos.color = color;
                Gizmos.DrawSphere(transform.position, 0.04f);
                Gizmos.color = origin;
            }
        }

        void OnDrawGizmos()
        {
            DrawSphere(bobberAnchor, Color.navyBlue);
            DrawSphere(hookAnchor, Color.softRed);
        }
    }
}