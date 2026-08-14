using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public partial class AppLauncher
    {
        public class Patch : LaunchStep
        {
            public override StepType Type => StepType.Patch;

            public override float Weight => 10f;

            public override async UniTask<bool> Run(CancellationToken cancellationToken = default)
            {
                Progress = 0f;

                float time = 0f;
                while (time < 1f)
                {
                    Progress = Mathf.InverseLerp(0, 1f, time);
                    time += Time.deltaTime;
                    await UniTask.NextFrame();
                }
                Progress = 1f;
                return true;
            }
        }
    }
}