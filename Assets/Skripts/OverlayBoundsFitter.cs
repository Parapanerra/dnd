using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class OverlayBoundsFitter : MonoBehaviour
{
    private const float WildShapeVerticalOffsetPixels = 24f;
    private const float WildShapeBottomPaddingPixels = 20f;
    private const float DefaultMinimumWideAspect = 0.75f;
    private const float VisibleAlphaThreshold = 0.001f;
    private const float OverlayHorizontalMovementFraction = 0.1f;
    [SerializeField] private float bottomPaddingPixels;
    [SerializeField] private float wideScreenVerticalOffsetPixels;
    [SerializeField] private float minimumWideAspect = DefaultMinimumWideAspect;

    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Dictionary<RectTransform, Vector2> appliedOffsets =
        new Dictionary<RectTransform, Vector2>();
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    [SerializeField] private MapCameraController mapBounds;

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
                SceneRoleMarker marker = canvas.GetComponent<SceneRoleMarker>();
                bool wildShape = marker != null
                    ? marker.Role == SceneRole.WildShapeOverlay
                    : canvas.name == "DFormOverLey";
                bool standard = marker != null
                    ? marker.Role == SceneRole.Overlay
                    : canvas.name == "overLey";
                float verticalOffset;
                if (wildShape)
                    verticalOffset = WildShapeVerticalOffsetPixels;
                else if (standard)
                    verticalOffset = 0f;
                else
                    continue;

                OverlayBoundsFitter fitter = canvas.GetComponent<OverlayBoundsFitter>();
                if (fitter == null)
                    fitter = canvas.gameObject.AddComponent<OverlayBoundsFitter>();

                fitter.bottomPaddingPixels = wildShape ? WildShapeBottomPaddingPixels : 0f;
                fitter.wideScreenVerticalOffsetPixels = verticalOffset;
                fitter.minimumWideAspect = DefaultMinimumWideAspect;
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
        if (mapBounds == null)
            mapBounds = FindSceneMapBounds();
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
        float horizontalParallaxOffset = GetHorizontalParallaxOffset();

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
                bounds.xMax) + horizontalParallaxOffset;

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

    private float GetHorizontalParallaxOffset()
    {
        if (targetCanvas == null || targetCanvas.renderMode == RenderMode.WorldSpace)
            return 0f;

        if (mapBounds == null)
            mapBounds = FindSceneMapBounds();

        Camera camera = Camera.main;
        if (mapBounds == null || camera == null || !camera.orthographic)
            return 0f;

        float canvasUnitsPerWorldUnit = canvasRect.rect.height /
            (camera.orthographicSize * 2f);
        return -mapBounds.HorizontalPanWorldDelta *
            canvasUnitsPerWorldUnit * OverlayHorizontalMovementFraction;
    }

    // Legacy fallback for scenes without the serialized camera binding.
    private MapCameraController FindSceneMapBounds()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            MapCameraController controller = root.GetComponentInChildren<MapCameraController>(true);
            if (controller != null) return controller;
        }
        return null;
    }

    private float GetWideScreenVerticalOffset()
    {
        if (wideScreenVerticalOffsetPixels == 0f ||
            (float)Screen.width / Mathf.Max(1, Screen.height) < minimumWideAspect)
        {
            return 0f;
        }

        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace)
            return wideScreenVerticalOffsetPixels * canvasRect.rect.height / Mathf.Max(1, Screen.height);

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
            if (!graphic.enabled || graphic.color.a <= VisibleAlphaThreshold)
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
        Rect safeArea = Screen.safeArea;
        safeArea.yMin = Mathf.Min(safeArea.yMax, safeArea.yMin + bottomPaddingPixels);

        if (mapBounds == null)
            mapBounds = FindAnyObjectByType<MapCameraController>();

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

        // Screen-space canvas coordinates follow the screen, even while its camera moves.
        // A camera-based conversion here can use the canvas transform from the previous
        // frame and make the fitted elements trail behind a camera pan.
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            Rect rect = canvasRect.rect;
            float xScale = rect.width / Mathf.Max(1, Screen.width);
            float yScale = rect.height / Mathf.Max(1, Screen.height);
            return Rect.MinMaxRect(
                rect.xMin + safeArea.xMin * xScale,
                rect.yMin + safeArea.yMin * yScale,
                rect.xMin + safeArea.xMax * xScale,
                rect.yMin + safeArea.yMax * yScale);
        }

        bool hasMin = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.min,
            targetCanvas != null ? targetCanvas.worldCamera : null,
            out Vector2 minLocal);
        bool hasMax = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            safeArea.max,
            targetCanvas != null ? targetCanvas.worldCamera : null,
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
