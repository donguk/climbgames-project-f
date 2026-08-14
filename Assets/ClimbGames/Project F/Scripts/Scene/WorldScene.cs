using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames.ProjectF
{
    public class WorldScene : MonoScene
    {
        void Start()
        {
            StartIfNotCameFromInitScene();
        }

        public override UniTask InitializeAsync()
        {
            return base.InitializeAsync();
        }

        void StartIfNotCameFromInitScene()
        {
            if (InitSceneFlag.DidCameFromInitScene == false)
            {
            }
        }
    }
}