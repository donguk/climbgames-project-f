#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace ClimbGames.ProjectF
{
    [InitializeOnLoad]
    public static class ProjectInitializer
    {
        private const string DEFAULT_SCENE_PATH = "Assets/ClimbGames/Project F/Scenes/00_Entry.unity";
        private const string SESSION_KEY = "EntrySceneLoader_HasExecuted";

        static ProjectInitializer()
        {
            // 이번 에디터 실행 세션에서 이미 실행된 적이 있다면 동작하지 않음
            if (SessionState.GetBool(SESSION_KEY, false))
                return;

            EditorApplication.delayCall += () =>
            {
                // 실행 직후 플래그를 true로 변경하여 재컴파일 시 재실행 방지
                SessionState.SetBool(SESSION_KEY, true);

                var activeScene = EditorSceneManager.GetActiveScene();
                bool isFirstRunState = string.IsNullOrEmpty(activeScene.path) ||
                                   activeScene.name == "Untitled" ||
                                   activeScene.name == "SampleScene";

                if (isFirstRunState)
                {
                    // 1. Android 플랫폼으로 스위칭 (Android가 아닐 경우만 실행)
                    if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                    {
                        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                    }

                    // 2. 지정한 기본 씬으로 전환
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