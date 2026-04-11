using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public partial class AppLauncher
    {
        public class VersionCheck : LaunchStep
        {
            public override StepType Type => StepType.VersionCheck;

            public override UniTask<bool> Run(CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(true);
            }
        }
    }
}