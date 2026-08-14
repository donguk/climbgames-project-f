using ClimbGames.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ClimbGames.ProjectF
{
    public class UIFishingSpot : UIBase, IPointerClickHandler
    {
        private FishingSpot spot;

        public void Setup(FishingSpot spot)
        {
            this.spot = spot;

            Vector3 position = spot.transform.position;
            position.y += 2f;
            transform.position = position;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            WorldManager.Instance.TransitionToFishing(spot).Forget();
        }
    }
}