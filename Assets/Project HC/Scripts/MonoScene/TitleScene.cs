using Cysharp.Threading.Tasks;
using ClimbGames.UI;

namespace ClimbGames.Project_HC
{
    public class TitleScene : MonoScene
    {
        private AppLauncher appLauncher;

        public override async UniTask InitializeAsync()
        {
            appLauncher = new AppLauncher();

            var data = new UITitle_Data()
            {
                appLauncher = appLauncher,
            };
            await UIManager.Instance.Show<UITitle>("UITitle", data, UILayer.View);
        }

        public override UniTask ActivateAsync()
        {
            appLauncher.Start();
            return base.ActivateAsync();
        }

        public override void Deactivate()
        {
            UIManager.Instance.Hide<UITitle>();

            appLauncher.Clear();
            appLauncher = null;
        }
    }
}