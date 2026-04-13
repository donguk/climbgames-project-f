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

        public Observable<Unit> OnClickFishingAsObservable() => btn_fishing.OnClickAsObservable();
    }
}