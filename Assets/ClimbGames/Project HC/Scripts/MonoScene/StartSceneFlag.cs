using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneFlag : MonoBehaviour
{
    private static StartSceneFlag instance;
    private static string startSceneName;

    public static bool DidCameFromInitScene => instance != null && startSceneName == "00_Entry";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void Initialize()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        startSceneName = activeScene.name;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    //void OnDestroy()
    //{
    //    instance = null;
    //}
}
