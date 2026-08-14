using Cysharp.Threading.Tasks;
using ClimbGames.UI;

namespace ClimbGames.ProjectF
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
            await UIManager.Instance.ShowUI<UITitle>("UITitle", UILayer.View, data);
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