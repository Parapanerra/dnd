using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Covers fields in every scene, including UI created after a scene loads.
internal static class InputFieldMotionInstaller
{
    private static float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Canvas.willRenderCanvases -= ScanForRuntimeFields;
        Canvas.willRenderCanvases += ScanForRuntimeFields;
        nextScan = 0f;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void ScanForRuntimeFields()
    {
        if (Time.unscaledTime < nextScan)
            return;

        nextScan = Time.unscaledTime + 1f;
        Install();
    }

    private static void Install()
    {
        foreach (InputField field in Object.FindObjectsByType<InputField>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Add(field.gameObject);

        foreach (TMP_InputField field in Object.FindObjectsByType<TMP_InputField>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
            Add(field.gameObject);
    }

    private static void Add(GameObject target)
    {
        if (target.GetComponent<InputFieldMotion>() == null)
            target.AddComponent<InputFieldMotion>();
    }
}
