using ClimbGames.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace ClimbGames.Project_HC
{
    public class FishingPoint : MonoBehaviour
    {
        [SerializeField] private Transform model;

        private UIBase uiMenu, uiIndicator;

        void Awake()
        {
            uiIndicator = UIManager.Instance.CreateUI<UIBase>("UIFisingArea", UILayer.World);
            Vector3 position = model.position;
            position.y += 2f;
            uiIndicator.transform.position = position;
        }

        void OnDestroy()
        {
            if (uiMenu != null)
                UIManager.HideSafely(uiMenu);

            if (uiIndicator != null)
                UIManager.HideSafely(uiIndicator);
        }

        void OnTriggerEnter(Collider other)
        {
            if (uiMenu == null)
                uiMenu = UIManager.Instance.CreateUI<UIMenu>("UIMenu", UILayer.HUD);
        }

        void OnTriggerExit(Collider other)
        {
            if (uiMenu != null)
                UIManager.Instance.Hide(uiMenu);

            uiMenu = null;
        }
    }
}