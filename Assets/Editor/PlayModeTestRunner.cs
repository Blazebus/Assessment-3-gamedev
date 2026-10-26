using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayModeTestRunner
{
    static PlayModeTestRunner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetString("PlayModeTest.State", "InPlayMode");
            EditorApplication.update += RunTest;
        }
    }

    private static float? startTime;
    private static void RunTest()
    {
        if (startTime == null) {
            startTime = (float)EditorApplication.timeSinceStartup;
        }

        if (EditorApplication.timeSinceStartup - startTime < 1.0f) return;

        EditorApplication.update -= RunTest;
        
        var gm = Object.FindAnyObjectByType<GameManager>();
        string resultStr = "No GM";
        if (gm != null)
        {
            bool gameOverVisible = gm.gameOverOverlay != null && gm.gameOverOverlay.activeSelf;
            resultStr = "Active: " + gm.isGameActive + ", Lives: " + gm.lives + ", Pellets: " + gm.pelletsCollected + "/" + gm.totalPellets + ", GameOver: " + gameOverVisible;
        }

        SessionState.SetString("PlayModeTest.Result", resultStr);
        SessionState.SetString("PlayModeTest.State", "Done");
        EditorApplication.isPlaying = false;
    }
}
