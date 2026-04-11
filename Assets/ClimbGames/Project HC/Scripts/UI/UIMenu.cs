using ClimbGames.UI;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ClimbGames.Project_HC
{
    public class UIMenu : UIBase
    {
        [SerializeField] private Button btn_fishing;

        protected override UniTask OnInitialize()
        {
            btn_fishing.OnClickAsObservable().Subscribe(_ => OnClickFishing()).AddTo(this);
            return base.OnInitialize();
        }

        void OnClickFishing()
        {
            Debug.Log("[UIMenu] OnClickFishing");
        }
    }
}