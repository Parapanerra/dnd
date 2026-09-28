using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class zoomCam : MonoBehaviour
{
    Vector3 touchStart;
    public float zoomMin = AppConfig.Input.DefaultMinimumZoom;
    public float zoomMax = AppConfig.Input.DefaultMaximumZoom;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public List<GameObject> scrollViews = new List<GameObject>(); // Список всех скролл вью в сцене

    private const int BackgroundSortingOrder = -2;
    private const float BackgroundOverscan = 1.05f;
    private readonly Dictionary<SpriteRenderer, Vector2> adaptiveBackgrounds =
        new Dictionary<SpriteRenderer, Vector2>();

    void Start()
    {
        FindAdaptiveBackgrounds();

        // Убедиться, что камера находится внутри границ при запуске
        Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
        FitAdaptiveBackgrounds();
    }

    void Update()
    {
        // Если хотя бы один скролл вью активен, не обрабатываем ввод для камеры
        if (IsAnyScrollViewOpen()) return;

        // Обработка начала касания или нажатия мыши
        if (Input.GetMouseButtonDown(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (Input.GetMouseButtonDown(0))
            {
                touchStart = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.touchCount == 1)
            {
                touchStart = Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            }
        }

        // Обработка зума при двух касаниях
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroLastPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOneLastPos = touchOne.position - touchOne.deltaPosition;

            float distTouch = (touchZeroLastPos - touchOneLastPos).magnitude;
            float currentDistTouch = (touchZero.position - touchOne.position).magnitude;

            float difference = currentDistTouch - distTouch;

            zoom(difference * AppConfig.Input.PinchZoomSensitivity);
        }
        // Обработка перемещения камеры при перетаскивании
        else if (Input.GetMouseButton(0) || (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved))
        {
            Vector3 direction = Vector3.zero;

            if (Input.GetMouseButton(0))
            {
                direction = touchStart - Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
            else if (Input.touchCount == 1)
            {
                direction = touchStart - Camera.main.ScreenToWorldPoint(Input.GetTouch(0).position);
            }

            Vector3 newPosition = Camera.main.transform.position + direction;
            Camera.main.transform.position = ClampCamera(newPosition);
        }

        // Обработка зума при прокрутке мыши
        zoom(Input.GetAxis("Mouse ScrollWheel"));
    }

    void LateUpdate()
    {
        FitAdaptiveBackgrounds();
    }

    void zoom(float increment)
    {
        float newSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomMin, zoomMax);
        Camera.main.orthographicSize = newSize;
        Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
    }

    Vector3 ClampCamera(Vector3 targetPosition)
    {
        float cameraHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float cameraHalfHeight = Camera.main.orthographicSize;

        float boundsWidth = maxBounds.x - minBounds.x;
        float boundsHeight = maxBounds.y - minBounds.y;
        float boundsCenterX = (minBounds.x + maxBounds.x) * 0.5f;
        float boundsCenterY = (minBounds.y + maxBounds.y) * 0.5f;

        float minX = minBounds.x + cameraHalfWidth;
        float maxX = maxBounds.x - cameraHalfWidth;
        float minY = minBounds.y + cameraHalfHeight;
        float maxY = maxBounds.y - cameraHalfHeight;

        float clampedX = cameraHalfWidth * 2f >= boundsWidth
            ? boundsCenterX
            : Mathf.Clamp(targetPosition.x, minX, maxX);

        float clampedY = cameraHalfHeight * 2f >= boundsHeight
            ? boundsCenterY
            : Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPosition.z);
    }

    void FindAdaptiveBackgrounds()
    {
        adaptiveBackgrounds.Clear();

        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (SpriteRenderer spriteRenderer in renderers)
        {
            if (spriteRenderer.sortingOrder != BackgroundSortingOrder)
                continue;

            Vector2 originalSize = spriteRenderer.size;
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            adaptiveBackgrounds.Add(spriteRenderer, originalSize);
        }
    }

    void FitAdaptiveBackgrounds()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null || !targetCamera.orthographic)
            return;

        foreach (KeyValuePair<SpriteRenderer, Vector2> background in adaptiveBackgrounds)
        {
            SpriteRenderer spriteRenderer = background.Key;
            if (spriteRenderer == null)
                continue;

            float worldScaleX = Mathf.Abs(spriteRenderer.transform.lossyScale.x);
            if (worldScaleX <= Mathf.Epsilon)
                continue;

            float cameraHalfWidth = targetCamera.orthographicSize * targetCamera.aspect * BackgroundOverscan;
            float distanceFromCameraCenter = Mathf.Abs(
                targetCamera.transform.position.x - spriteRenderer.bounds.center.x);
            float requiredWorldWidth = (cameraHalfWidth + distanceFromCameraCenter) * 2f;

            Vector2 newSize = spriteRenderer.size;
            newSize.x = Mathf.Max(background.Value.x, requiredWorldWidth / worldScaleX);
            newSize.y = background.Value.y;
            spriteRenderer.size = newSize;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(minBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, minBounds.y, 0));
        Gizmos.DrawLine(new Vector3(maxBounds.x, minBounds.y, 0), new Vector3(maxBounds.x, maxBounds.y, 0));
        Gizmos.DrawLine(new Vector3(maxBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, maxBounds.y, 0));
        Gizmos.DrawLine(new Vector3(minBounds.x, maxBounds.y, 0), new Vector3(minBounds.x, minBounds.y, 0));
    }

    // Метод для добавления скролл вью в список
    public void AddScrollView(GameObject scrollView)
    {
        if (!scrollViews.Contains(scrollView))
        {
            scrollViews.Add(scrollView);
        }
    }

    // Метод для удаления скролл вью из списка
    public void RemoveScrollView(GameObject scrollView)
    {
        if (scrollViews.Contains(scrollView))
        {
            scrollViews.Remove(scrollView);
        }
    }

    // Метод для проверки, открыт ли хотя бы один скролл вью
    bool IsAnyScrollViewOpen()
    {
        foreach (GameObject scrollView in scrollViews)
        {
            if (scrollView.activeSelf)
            {
                return true;
            }
        }
        return false;
    }
}
