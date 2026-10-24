using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class ButtonMotionInstaller
{
    private static float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Canvas.willRenderCanvases -= ScanForRuntimeButtons;
        Canvas.willRenderCanvases += ScanForRuntimeButtons;
        nextScan = 0f;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void ScanForRuntimeButtons()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 1f;
        Install();
    }

    private static void Install()
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Button button in buttons)
            if (button.GetComponent<ButtonMotion>() == null)
                button.gameObject.AddComponent<ButtonMotion>();
    }
}
