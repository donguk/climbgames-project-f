using System.Threading;
using Cysharp.Threading.Tasks;

namespace ClimbGames.Project_HC
{
    public partial class AppLauncher
    {
        public class UserPermission : LaunchStep
        {
            public override StepType Type => StepType.UserPermission;

            public override UniTask<bool> Run(CancellationToken cancellationToken = default)
            {
                return UniTask.FromResult(true);
            }
        }
    }
}