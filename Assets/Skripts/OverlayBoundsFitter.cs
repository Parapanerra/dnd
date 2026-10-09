using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class OverlayBoundsFitter : MonoBehaviour
{
    [SerializeField] private float bottomPaddingPixels;
    [SerializeField] private float wideScreenVerticalOffsetPixels;
    [SerializeField] private float minimumWideAspect = 0.75f;

    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Dictionary<RectTransform, Vector2> appliedOffsets =
        new Dictionary<RectTransform, Vector2>();
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private zoomCam mapBounds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -= ConfigureSceneOverlays;
        SceneManager.sceneLoaded += ConfigureSceneOverlays;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ConfigureActiveScene()
    {
        ConfigureSceneOverlays(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void ConfigureSceneOverlays(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                float verticalOffset;
                if (canvas.name == "DFormOverLey")
                    verticalOffset = 24f;
                else if (canvas.name == "overLey")
                    verticalOffset = 0f;
                else
                    continue;

                OverlayBoundsFitter fitter = canvas.GetComponent<OverlayBoundsFitter>();
                if (fitter == null)
                    fitter = canvas.gameObject.AddComponent<OverlayBoundsFitter>();

                fitter.bottomPaddingPixels = canvas.name == "DFormOverLey" ? 20f : 0f;
                fitter.wideScreenVerticalOffsetPixels = verticalOffset;
                fitter.minimumWideAspect = 0.75f;
                fitter.RefreshReferences();
            }
        }
    }

    private void Awake()
    {
        RefreshReferences();
    }

    private void RefreshReferences()
    {
        targetCanvas = GetComponent<Canvas>();
        canvasRect = transform as RectTransform;
        mapBounds = FindFirstObjectByType<zoomCam>();
    }

    private void LateUpdate()
    {
        KeepVisibleElementsInsideCanvas();
    }

    private void KeepVisibleElementsInsideCanvas()
    {
        if (canvasRect == null)
            return;

        Rect bounds = GetVisibleBounds();

        for (int i = 0; i < canvasRect.childCount; i++)
        {
            RectTransform child = canvasRect.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeInHierarchy)
                continue;

            if (child.GetComponent<Graphic>() == null)
                continue;

            if (appliedOffsets.TryGetValue(child, out Vector2 previousOffset))
                child.anchoredPosition -= previousOffset;

            GetVisibleGroupBounds(child, out Vector2 bottomLeft, out Vector2 topRight);

            float horizontalOffset = GetAxisOffset(
                bottomLeft.x,
                topRight.x,
                bounds.xMin,
                bounds.xMax);

            float verticalOffset = GetAxisOffset(
                bottomLeft.y,
                topRight.y,
                bounds.yMin,
                bounds.yMax);

            verticalOffset += GetWideScreenVerticalOffset();

            if (!Mathf.Approximately(horizontalOffset, 0f) ||
                !Mathf.Approximately(verticalOffset, 0f))
            {
                Vector2 offset = new Vector2(horizontalOffset, verticalOffset);
                child.anchoredPosition += offset;
                appliedOffsets[child] = offset;
            }
            else
            {
                appliedOffsets[child] = Vector2.zero;
            }
        }
    }

    private float GetWideScreenVerticalOffset()
    {
        if (wideScreenVerticalOffsetPixels == 0f ||
            (float)Screen.width / Mathf.Max(1, Screen.height) < minimumWideAspect)
        {
            return 0f;
        }

        Camera canvasCamera = targetCanvas != null &&
                              targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        bool hasStart = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            Vector2.zero,
            canvasCamera,
            out Vector2 start);
        bool hasEnd = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            new Vector2(0f, wideScreenVerticalOffsetPixels),
            canvasCamera,
            out Vector2 end);

        return hasStart && hasEnd ? end.y - start.y : wideScreenVerticalOffsetPixels;
    }

    private void GetVisibleGroupBounds(
        RectTransform group,
        out Vector2 bottomLeft,
        out Vector2 topRight)
    {
        bottomLeft = new Vector2(float.MaxValue, float.MaxValue);
        topRight = new Vector2(float.MinValue, float.MinValue);
        bool foundGraphic = false;

        Graphic[] graphics = group.GetComponentsInChildren<Graphic>(false);
        foreach (Graphic graphic in graphics)
        {
            if (!graphic.enabled || graphic.color.a <= 0.001f)
                continue;

            graphic.rectTransform.GetWorldCorners(worldCorners);
            for (int i = 0; i < worldCorners.Length; i++)
            {
                Vector3 localCorner = canvasRect.InverseTransformPoint(worldCorners[i]);
                bottomLeft = Vector2.Min(bottomLeft, localCorner);
                topRight = Vector2.Max(topRight, localCorner);
                foundGraphic = true;
            }
        }

        if (foundGraphic)
            return;

        group.GetWorldCorners(worldCorners);
        bottomLeft = canvasRect.InverseTransformPoint(worldCorners[0]);
        topRight = canvasRect.InverseTransformPoint(worldCorners[2]);
    }

    private Rect GetVisibleBounds()
    {
        return GetScreenBoundsInCanvas();
    }

    private Rect GetScreenBoundsInCanvas()
    {
        Camera canvasCamera = targetCanvas != null &&
                              targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? targetCanvas.worldCamera
            : null;

        Rect safeArea = Screen.safeArea;
        safeArea.yMin = Mathf.Min(safeArea.yMax, safeArea.yMin + bottomPaddingPixels);

        if (mapBounds == null)
            mapBounds = FindFirstObjectByType<zoomCam>();

        if (mapBounds != null)
        {
            float mapWidth = mapBounds.MaxBounds.x - mapBounds.MinBounds.x;
            float mapHeight = mapBounds.MaxBounds.y - mapBounds.MinBounds.y;

            if (mapWidth > 0f && mapHeight > 0f)
            {
                float maximumWidth = safeArea.height * (mapWidth / mapHeight);
                if (safeArea.width > maximumWidth)
                {
                    float horizontalInset = (safeArea.width - maximumWidth) * 0.5f;
                    safeArea.xMin += horizontalInset;
                    safeArea.xMax -= horizontalInset;
                }
            }
        }

        bool hasMin = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.min,
            canvasCamera,
            out Vector2 minLocal);
        bool hasMax = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.max,
            canvasCamera,
            out Vector2 maxLocal);

        if (!hasMin || !hasMax)
            return canvasRect.rect;

        return Rect.MinMaxRect(
            Mathf.Min(minLocal.x, maxLocal.x),
            Mathf.Min(minLocal.y, maxLocal.y),
            Mathf.Max(minLocal.x, maxLocal.x),
            Mathf.Max(minLocal.y, maxLocal.y));
    }

    private static float GetAxisOffset(
        float elementMin,
        float elementMax,
        float boundsMin,
        float boundsMax)
    {
        float elementSize = elementMax - elementMin;
        float boundsSize = boundsMax - boundsMin;

        if (elementSize > boundsSize)
            return (boundsMin + boundsMax - elementMin - elementMax) * 0.5f;

        if (elementMin < boundsMin)
            return boundsMin - elementMin;

        if (elementMax > boundsMax)
            return boundsMax - elementMax;

        return 0f;
    }
}
