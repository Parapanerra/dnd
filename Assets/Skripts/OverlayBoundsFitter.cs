using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class OverlayBoundsFitter : MonoBehaviour
{
    private readonly Vector3[] worldCorners = new Vector3[4];
    private readonly Dictionary<RectTransform, Vector2> appliedOffsets =
        new Dictionary<RectTransform, Vector2>();
    private Canvas targetCanvas;
    private RectTransform canvasRect;
    private zoomCam mapBounds;

    private void Awake()
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

            child.GetWorldCorners(worldCorners);

            Vector3 bottomLeft = canvasRect.InverseTransformPoint(worldCorners[0]);
            Vector3 topRight = canvasRect.InverseTransformPoint(worldCorners[2]);

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

        if (mapBounds == null)
            mapBounds = FindFirstObjectByType<zoomCam>();

        if (mapBounds != null)
        {
            float mapWidth = mapBounds.maxBounds.x - mapBounds.minBounds.x;
            float mapHeight = mapBounds.maxBounds.y - mapBounds.minBounds.y;

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
