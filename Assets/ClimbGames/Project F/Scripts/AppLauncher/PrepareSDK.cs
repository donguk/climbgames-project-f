using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace ClimbGames.ProjectF
{
    public partial class AppLauncher
    {
        public class PrepareSDK : LaunchStep
        {
            public override StepType Type => StepType.PrepareSDK;

            public override UniTask<bool> Run(CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(true); ;
            }
        }
    }
}