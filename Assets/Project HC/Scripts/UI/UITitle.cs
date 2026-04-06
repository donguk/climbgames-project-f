using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ClimbGames.UI;
using R3;

namespace ClimbGames.Project_HC
{
    public struct UITitle_Data : IUIData
    {
        public AppLauncher appLauncher;
    }

    public class UITitle : UIBase<UITitle_Data>
    {
        [SerializeField] private Slider sld_progress;
        [SerializeField] private TMP_Text txt_progress;
        [SerializeField] private Button btn_enter;

        private AppLauncher appLauncher;

        protected override void OnInitialize(UITitle_Data data)
        {
            appLauncher = data.appLauncher;

            sld_progress.value = 0f;
            txt_progress.text = string.Empty;
            btn_enter.gameObject.SetActive(false);

            btn_enter.OnClickAsObservable().Subscribe(_ => SceneTransition.TransitionAsync("02_World")).AddTo(this);
        }

        void Update()
        {
            if (appLauncher != null)
            {
                if (appLauncher.IsRunning)
                {
                    sld_progress.value = appLauncher.ProgressValue;
                    txt_progress.text = appLauncher.ProgressText;
                }

                if (btn_enter.gameObject.activeSelf == false && appLauncher.Result)
                    btn_enter.gameObject.SetActive(true);
            }
        }
    }
}