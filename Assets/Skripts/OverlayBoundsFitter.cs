using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class OverlayBoundsFitter : MonoBehaviour
{
    private const float LinkedGroupGapPixels = 8f;
    private const float DefaultMinimumWideAspect = 0.75f;
    private const float ScaleTransitionStartAspect = 1f;
    private const float ScaleTransitionEndAspect = 1.5f;
    private const float WideScaleMultiplier = 1.2f;
    private const float VisibleAlphaThreshold = 0.001f;
    private const float WideScreenSidePaddingFraction = 0.29f;
    [SerializeField] private float bottomPaddingPixels;
    [SerializeField] private float wideScreenVerticalOffsetPixels;
    [SerializeField] private float minimumWideAspect = DefaultMinimumWideAspect;

    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Dictionary<RectTransform, Vector2> appliedOffsets =
        new Dictionary<RectTransform, Vector2>();
    private readonly HashSet<RectTransform> linkedControls = new HashSet<RectTransform>();
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private CanvasScaler canvasScaler;
    private Vector2 originalReferenceResolution;
    private float maximumWideScaleMultiplier = 1f;
    private bool wildShapeLayoutApplied;

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
                bool isOverlay = marker != null
                    ? marker.Role == SceneRole.Overlay ||
                      marker.Role == SceneRole.WildShapeOverlay
                    : canvas.name == "overLey" || canvas.name == "DFormOverLey";
                if (!isOverlay)
                    continue;

                OverlayBoundsFitter fitter = canvas.GetComponent<OverlayBoundsFitter>();
                if (fitter == null)
                    fitter = canvas.gameObject.AddComponent<OverlayBoundsFitter>();

                fitter.bottomPaddingPixels = 0f;
                fitter.wideScreenVerticalOffsetPixels = 0f;
                fitter.minimumWideAspect = DefaultMinimumWideAspect;
                fitter.maximumWideScaleMultiplier = WideScaleMultiplier;
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
        if (canvasScaler == null)
        {
            canvasScaler = GetComponent<CanvasScaler>();
            if (canvasScaler != null)
                originalReferenceResolution = canvasScaler.referenceResolution;
        }
    }

    private void LateUpdate()
    {
        if (IsWildShapeOverlay())
        {
            ApplyWildShapeSceneLayout();
            if (IsNarrowWildShapeCanvas())
                KeepVisibleElementsInsideCanvas();
            else
                RestoreAllOffsets();
            return;
        }

        FitOverlayScale();
        KeepVisibleElementsInsideCanvas();
    }

    private bool IsWildShapeOverlay()
    {
        SceneRoleMarker marker = GetComponent<SceneRoleMarker>();
        return gameObject.scene.name == "petsesn" &&
               marker != null && marker.Role == SceneRole.WildShapeOverlay;
    }

    private bool IsNarrowWildShapeCanvas()
    {
        return canvasRect != null && canvasRect.rect.height > 0f &&
               canvasRect.rect.width / canvasRect.rect.height < minimumWideAspect;
    }

    private void RestoreAllOffsets()
    {
        foreach (KeyValuePair<RectTransform, Vector2> entry in appliedOffsets)
        {
            if (entry.Key != null)
                entry.Key.anchoredPosition -= entry.Value;
        }
        appliedOffsets.Clear();
    }

    private void ApplyWildShapeSceneLayout()
    {
        if (canvasScaler != null)
            canvasScaler.referenceResolution = new Vector2(920f, 1990f);

        if (wildShapeLayoutApplied)
            return;

        SetSceneRect("menuPanel", -1249f, -691f, 353f, 513f);
        SetSceneRect("menuPanel/menuPanel (1)", -477f, -307f, 270f, 608.51f);
        SetSceneRect("menuBatton", -1137.00024f, -420.000061f, 100f, 100f);
        SetSceneRect("kalPanel", -1300.84521f, -1372.5f, 441.420044f, 793.17f);
        SetSceneRect("kalBatton", -1154.84521f, -971.000061f, 100f, 100f);
        wildShapeLayoutApplied = true;
    }

    private void SetSceneRect(string path, float x, float y, float width, float height)
    {
        RectTransform rect = canvasRect != null ? canvasRect.Find(path) as RectTransform : null;
        if (rect == null)
            return;

        Vector2 topRight = Vector2.one;
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 position = new Vector2(x, y);
        Vector2 size = new Vector2(width, height);
        if (rect.anchorMin != topRight) rect.anchorMin = topRight;
        if (rect.anchorMax != topRight) rect.anchorMax = topRight;
        if (rect.pivot != center) rect.pivot = center;
        if (rect.sizeDelta != size) rect.sizeDelta = size;
        if (rect.anchoredPosition != position) rect.anchoredPosition = position;
    }

    private void OnDisable()
    {
        if (canvasScaler != null && originalReferenceResolution.x > 0f &&
            originalReferenceResolution.y > 0f)
            canvasScaler.referenceResolution = originalReferenceResolution;
    }

    private void FitOverlayScale()
    {
        if (canvasScaler == null ||
            canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
            originalReferenceResolution.x <= 0f || originalReferenceResolution.y <= 0f)
            return;

        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        float wideBlend = Mathf.InverseLerp(
            ScaleTransitionStartAspect, ScaleTransitionEndAspect, aspect);
        Vector2 referenceResolution = new Vector2(originalReferenceResolution.x,
            Mathf.Lerp(originalReferenceResolution.y,
                originalReferenceResolution.y / maximumWideScaleMultiplier, wideBlend));
        if (canvasScaler.referenceResolution != referenceResolution)
            canvasScaler.referenceResolution = referenceResolution;
    }

    private void KeepVisibleElementsInsideCanvas()
    {
        if (canvasRect == null)
            return;

        Rect bounds = GetVisibleBounds();
        linkedControls.Clear();
        FitLinkedGroup("menuPanel", "menuBatton", bounds, linkedControls);
        FitLinkedGroup("kalPanel", "kalBatton", bounds, linkedControls);
        KeepLinkedGroupsApart(bounds);

        for (int i = 0; i < canvasRect.childCount; i++)
        {
            RectTransform child = canvasRect.GetChild(i) as RectTransform;
            if (child == null || linkedControls.Contains(child) ||
                !child.gameObject.activeInHierarchy)
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

    private void FitLinkedGroup(string panelName, string buttonName, Rect bounds,
        HashSet<RectTransform> linkedControls)
    {
        RectTransform panel = canvasRect.Find(panelName) as RectTransform;
        RectTransform button = canvasRect.Find(buttonName) as RectTransform;
        if (panel == null || button == null)
            return;

        linkedControls.Add(panel);
        linkedControls.Add(button);
        RestorePreviousOffset(panel);
        RestorePreviousOffset(button);

        GetVisibleGroupBounds(panel, out Vector2 panelMin, out Vector2 panelMax);
        GetVisibleGroupBounds(button, out Vector2 buttonMin, out Vector2 buttonMax);
        Vector2 groupMin = Vector2.Min(panelMin, buttonMin);
        Vector2 groupMax = Vector2.Max(panelMax, buttonMax);
        float horizontalOffset = IsWildShapeOverlay() && IsNarrowWildShapeCanvas()
            ? bounds.xMax - groupMax.x
            : GetAxisOffset(groupMin.x, groupMax.x, bounds.xMin, bounds.xMax);
        Vector2 offset = new Vector2(
            horizontalOffset,
            GetAxisOffset(groupMin.y, groupMax.y, bounds.yMin, bounds.yMax) +
                GetWideScreenVerticalOffset());

        ApplyOffset(panel, offset);
        ApplyOffset(button, offset);
    }

    private void RestorePreviousOffset(RectTransform child)
    {
        if (appliedOffsets.TryGetValue(child, out Vector2 previousOffset))
            child.anchoredPosition -= previousOffset;
    }

    private void ApplyOffset(RectTransform child, Vector2 offset)
    {
        child.anchoredPosition += offset;
        appliedOffsets[child] = offset;
    }

    private void KeepLinkedGroupsApart(Rect bounds)
    {
        RectTransform menu = canvasRect.Find("menuPanel") as RectTransform;
        RectTransform menuButton = canvasRect.Find("menuBatton") as RectTransform;
        RectTransform calculator = canvasRect.Find("kalPanel") as RectTransform;
        RectTransform calculatorButton = canvasRect.Find("kalBatton") as RectTransform;
        if (menu == null || menuButton == null || calculator == null || calculatorButton == null)
            return;

        GetVisibleGroupBounds(menu, out Vector2 menuPanelMin, out Vector2 menuPanelMax);
        GetVisibleGroupBounds(menuButton, out Vector2 menuButtonMin, out Vector2 menuButtonMax);
        GetVisibleGroupBounds(calculator, out Vector2 calculatorPanelMin, out Vector2 calculatorPanelMax);
        GetVisibleGroupBounds(calculatorButton, out Vector2 calculatorButtonMin, out Vector2 calculatorButtonMax);

        RectTransform wildForms = menu.Find("menuPanel (1)") as RectTransform;
        if (wildForms != null)
        {
            GetVisibleGroupBounds(wildForms, out Vector2 formsMin, out _);
            float wildCalculatorBottom = Mathf.Min(calculatorPanelMin.y, calculatorButtonMin.y);
            float wildCalculatorTop = Mathf.Max(calculatorPanelMax.y, calculatorButtonMax.y);
            float requestedShift = formsMin.y - calculatorPanelMax.y;
            float visibleShift = Mathf.Clamp(requestedShift,
                bounds.yMin - wildCalculatorBottom, bounds.yMax - wildCalculatorTop);
            ShiftLinkedGroup(calculator, calculatorButton, visibleShift);
            return;
        }

        float menuBottom = Mathf.Min(menuPanelMin.y, menuButtonMin.y);
        float menuTop = Mathf.Max(menuPanelMax.y, menuButtonMax.y);
        float calculatorBottom = Mathf.Min(calculatorPanelMin.y, calculatorButtonMin.y);
        float calculatorTop = Mathf.Max(calculatorPanelMax.y, calculatorButtonMax.y);
        float gap = LinkedGroupGapPixels * canvasRect.rect.height / Mathf.Max(1, Screen.height);
        float requiredSeparation = calculatorTop + gap - menuBottom;
        float menuLift = 0f;
        float calculatorDrop = 0f;
        if (requiredSeparation > 0f)
        {
            menuLift = Mathf.Min(requiredSeparation, Mathf.Max(0f, bounds.yMax - menuTop));
            ShiftLinkedGroup(menu, menuButton, menuLift);
            float remaining = requiredSeparation - menuLift;
            calculatorDrop = Mathf.Min(remaining, Mathf.Max(0f, calculatorBottom - bounds.yMin));
            ShiftLinkedGroup(calculator, calculatorButton, -calculatorDrop);
        }

        // Keep the calculator button against the menu panel, moving its panel with it.
        float spaceAboveButton = menuPanelMin.y + menuLift -
                                 (calculatorButtonMax.y - calculatorDrop);
        if (spaceAboveButton > 0f)
            ShiftLinkedGroup(calculator, calculatorButton, spaceAboveButton);
    }

    private void ShiftLinkedGroup(RectTransform panel, RectTransform button, float verticalOffset)
    {
        if (Mathf.Approximately(verticalOffset, 0f))
            return;

        Vector2 offset = new Vector2(0f, verticalOffset);
        panel.anchoredPosition += offset;
        button.anchoredPosition += offset;
        appliedOffsets[panel] += offset;
        appliedOffsets[button] += offset;
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

        // Include a closed panel's normal children, but skip independently hidden
        // descendants and Dropdown templates. Unity can deactivate a template on
        // first use, which would otherwise move the fitted panel and its button.
        Graphic[] graphics = group.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (!graphic.enabled || graphic.color.a <= VisibleAlphaThreshold ||
                !IsActiveWithinGroup(graphic.transform, group))
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
        {
            // ButtonMotion scales overlay handles for feedback. Layout must use their
            // resting bounds, otherwise the handle moves its linked panel each frame.
            if (group.TryGetComponent(out ButtonMotion motion))
            {
                float scale = motion.LayoutScaleFactor;
                if (scale > 0f && !Mathf.Approximately(scale, 1f))
                {
                    Vector2 pivot = canvasRect.InverseTransformPoint(group.position);
                    bottomLeft = pivot + (bottomLeft - pivot) / scale;
                    topRight = pivot + (topRight - pivot) / scale;
                }
            }
            return;
        }

        group.GetWorldCorners(worldCorners);
        bottomLeft = canvasRect.InverseTransformPoint(worldCorners[0]);
        topRight = canvasRect.InverseTransformPoint(worldCorners[2]);
    }

    private static bool IsActiveWithinGroup(Transform element, Transform group)
    {
        for (Transform current = element; current != group; current = current.parent)
        {
            if (current == null || !current.gameObject.activeSelf)
                return false;

            if (current.name == "Template" && current.parent != null &&
                current is RectTransform template)
            {
                Dropdown dropdown = current.parent.GetComponent<Dropdown>();
                if (dropdown != null && dropdown.template == template)
                    return false;

                TMPro.TMP_Dropdown tmpDropdown =
                    current.parent.GetComponent<TMPro.TMP_Dropdown>();
                if (tmpDropdown != null && tmpDropdown.template == template)
                    return false;
            }
        }

        return true;
    }

    private Rect GetVisibleBounds()
    {
        return GetScreenBoundsInCanvas();
    }

    private Rect GetScreenBoundsInCanvas()
    {
        Rect safeArea = Screen.safeArea;
        float screenWidth = Mathf.Max(Screen.width, safeArea.xMax);
        float screenHeight = Mathf.Max(Screen.height, safeArea.yMax);
        safeArea.yMin = Mathf.Min(safeArea.yMax, safeArea.yMin + bottomPaddingPixels);

        // Leave a small fixed screen margin without following camera zoom or pan.
        if (safeArea.width > safeArea.height)
        {
            float inset = safeArea.width * WideScreenSidePaddingFraction;
            safeArea.xMin += inset;
            safeArea.xMax -= inset;
        }

        // Screen-space canvas coordinates follow the screen, even while its camera moves.
        // A camera-based conversion here can use the canvas transform from the previous
        // frame and make the fitted elements trail behind a camera pan.
        if (targetCanvas != null && targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            Rect rect = canvasRect.rect;
            float xScale = rect.width / Mathf.Max(1f, screenWidth);
            float yScale = rect.height / Mathf.Max(1f, screenHeight);
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
