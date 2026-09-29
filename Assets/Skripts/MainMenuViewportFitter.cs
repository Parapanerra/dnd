using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class MainMenuViewportFitter : MonoBehaviour
{
    [SerializeField] private float minimumWideAspect = 0.75f;

    private readonly string[] centeredObjectNames =
    {
        "menukart",
        "openPanelSaveBatton"
    };

    private readonly Dictionary<RectTransform, Vector2> originalPositions =
        new Dictionary<RectTransform, Vector2>();
    private readonly Vector3[] targetCorners = new Vector3[4];
    private Canvas targetCanvas;
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
                fitter.CacheTargets();
            }
        }
    }

    private static bool ContainsMainMenuObjects(Transform parent)
    {
        bool hasMenu = false;
        bool hasSaveButton = false;

        for (int i = 0; i < parent.childCount; i++)
        {
            string childName = parent.GetChild(i).name;
            hasMenu |= childName == "menukart";
            hasSaveButton |= childName == "openPanelSaveBatton";
        }

        return hasMenu && hasSaveButton;
    }

    private void Awake()
    {
        CacheTargets();
    }

    private void CacheTargets()
    {
        targetCanvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        if (canvasRect == null)
            return;

        for (int i = 0; i < canvasRect.childCount; i++)
        {
            RectTransform child = canvasRect.GetChild(i) as RectTransform;
            if (child != null && ShouldCenter(child.name) && !originalPositions.ContainsKey(child))
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

            if (useWideLayout)
                CenterOnScreen(item.Key);
            else
                item.Key.anchoredPosition = item.Value;
        }
    }

    private void CenterOnScreen(RectTransform target)
    {
        RectTransform parent = target.parent as RectTransform;
        if (parent == null)
            return;

        Camera canvasCamera = targetCanvas != null &&
                              targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        target.GetWorldCorners(targetCorners);

        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, targetCorners[0]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, targetCorners[2]);
        float currentCenterX = (bottomLeft.x + topRight.x) * 0.5f;
        float desiredCenterX = Screen.safeArea.center.x;
        float sampleY = (bottomLeft.y + topRight.y) * 0.5f;

        bool hasCurrent = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            new Vector2(currentCenterX, sampleY),
            canvasCamera,
            out Vector2 currentLocal);
        bool hasDesired = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            new Vector2(desiredCenterX, sampleY),
            canvasCamera,
            out Vector2 desiredLocal);

        if (!hasCurrent || !hasDesired)
            return;

        Vector2 position = target.anchoredPosition;
        position.x += desiredLocal.x - currentLocal.x;
        target.anchoredPosition = position;
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
