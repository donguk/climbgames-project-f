using ClimbGames.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using R3;

namespace ClimbGames.Project_HC
{
    public class FishingPoint : MonoBehaviour
    {
        [SerializeField] private Transform model;
        [SerializeField] private Transform rodRoot;
        [SerializeField] private Transform castingArea;

        private UIBase uiIndicator;
        private UIMenu uiMenu;

        public Transform RodRoot => rodRoot;
        public Vector3 CastingPosition => castingArea.position;

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
            {
                uiMenu = UIManager.Instance.CreateUI<UIMenu>("UIMenu", UILayer.HUD);
                uiMenu.OnClickFishingAsObservable().Subscribe(_ => WorldManager.Instance.TransitionToFishing(this).Forget()).AddTo(uiMenu);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (uiMenu != null)
                UIManager.Instance.Hide(uiMenu);

            uiMenu = null;
        }
    }
}