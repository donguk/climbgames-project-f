using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ClimbGames
{
    public class EntryScene : MonoScene
    {
        void Start()
        {
            Application.targetFrameRate = 60;

            SceneTransition.LoadAsync("01_Title").Forget();
        }
    }
}