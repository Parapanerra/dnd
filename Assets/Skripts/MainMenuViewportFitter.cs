using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MainMenuViewportFitter : MonoBehaviour
{
    [SerializeField] private float minimumWideAspect = 0.75f;
    [SerializeField] private float wideScreenOffset = -200f;

    private readonly string[] centeredObjectNames =
    {
        "menukart",
        "openPanelSavePanel",
        "openPanelSaveBatton"
    };

    private readonly Dictionary<RectTransform, Vector2> originalPositions =
        new Dictionary<RectTransform, Vector2>();
    private RectTransform canvasRect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= ConfigureScene;
        SceneManager.sceneLoaded += ConfigureScene;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureActiveScene()
    {
        ConfigureScene(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void ConfigureScene(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded || scene.name != "menu")
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (!ContainsMainMenuObjects(canvas.transform))
                    continue;

                MainMenuViewportFitter fitter = canvas.GetComponent<MainMenuViewportFitter>();
                if (fitter == null)
                    fitter = canvas.gameObject.AddComponent<MainMenuViewportFitter>();

                fitter.minimumWideAspect = 0.75f;
                fitter.wideScreenOffset = -200f;
                fitter.CacheTargets();
            }
        }
    }

    private static bool ContainsMainMenuObjects(Transform parent)
    {
        bool hasMenu = false;
        bool hasSavePanel = false;

        for (int i = 0; i < parent.childCount; i++)
        {
            string childName = parent.GetChild(i).name;
            hasMenu |= childName == "menukart";
            hasSavePanel |= childName == "openPanelSavePanel";
        }

        return hasMenu && hasSavePanel;
    }

    private void Awake()
    {
        CacheTargets();
    }

    private void CacheTargets()
    {
        canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        originalPositions.Clear();
        for (int i = 0; i < canvasRect.childCount; i++)
        {
            RectTransform child = canvasRect.GetChild(i) as RectTransform;
            if (child != null && ShouldCenter(child.name))
                originalPositions[child] = child.anchoredPosition;
        }
    }

    private void LateUpdate()
    {
        bool useWideLayout = (float)Screen.width / Mathf.Max(1, Screen.height) >= minimumWideAspect;

        foreach (KeyValuePair<RectTransform, Vector2> item in originalPositions)
        {
            if (item.Key == null)
                continue;

            Vector2 position = item.Value;
            if (useWideLayout)
                position.x += wideScreenOffset;

            item.Key.anchoredPosition = position;
        }
    }

    private bool ShouldCenter(string objectName)
    {
        for (int i = 0; i < centeredObjectNames.Length; i++)
        {
            if (objectName == centeredObjectNames[i])
                return true;
        }

        return false;
    }
}
