#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace ClimbGames.ProjectF
{
    [InitializeOnLoad]
    public static class EntrySceneLoader
    {
        private const string DEFAULT_SCENE_PATH = "Assets/ClimbGames/Project F/Scenes/00_Entry.unity";
        private const string SESSION_KEY = "EntrySceneLoader_HasExecuted";

        static EntrySceneLoader()
        {
            // 이번 에디터 실행 세션에서 이미 실행된 적이 있다면 동작하지 않음
            if (SessionState.GetBool(SESSION_KEY, false))
                return;

            EditorApplication.delayCall += () =>
            {
                // 실행 직후 플래그를 true로 변경하여 재컴파일 시 재실행 방지
                SessionState.SetBool(SESSION_KEY, true);

                var activeScene = EditorSceneManager.GetActiveScene();

                // 열린 씬이 없거나 SampleScene일 때만 지정한 기본 씬 오픈
                if (string.IsNullOrEmpty(activeScene.path) || activeScene.name == "Untitled")
                {
                    if (File.Exists(DEFAULT_SCENE_PATH))
                    {
                        EditorSceneManager.OpenScene(DEFAULT_SCENE_PATH);
                    }
                }
            };
        }
    }
}
#endif