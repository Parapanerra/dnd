using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapCameraController : MonoBehaviour
{
    Vector3 touchStart;
    [SerializeField] private float zoomMin = AppConfig.Input.DefaultMinimumZoom;
    [SerializeField] private float zoomMax = AppConfig.Input.DefaultMaximumZoom;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;
    [SerializeField, Range(0f, 0.5f)] private float formHorizontalTravelFraction = 0.2f;
    [SerializeField, Range(0f, 1f)] private float backgroundMovementFraction = 0.3f;
    public Vector2 MinBounds => minBounds;
    public Vector2 MaxBounds => maxBounds;
    [SerializeField] private List<GameObject> scrollViews = new List<GameObject>(); // Список всех скролл вью в сцене

    private readonly AdaptiveBackgroundFitter backgroundFitter = new AdaptiveBackgroundFitter();
    private readonly HorizontalBackgroundParallax backgroundParallax = new HorizontalBackgroundParallax();
    private readonly Vector3[] formCorners = new Vector3[4];
    private RectTransform mainForm;
    private float initialCameraX;
    private bool isStaticMenuCamera;

    internal float HorizontalPanWorldDelta => transform.position.x - initialCameraX;

    private void Awake()
    {
        initialCameraX = transform.position.x;
    }

    void Start()
    {
        isStaticMenuCamera = SceneManager.GetActiveScene().name == "menu";
        if (isStaticMenuCamera)
            return;

        backgroundFitter.Collect();
        mainForm = FindMainForm();
        initialCameraX = Camera.main.transform.position.x;

        // Убедиться, что камера находится внутри границ при запуске
        Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
        backgroundParallax.Collect(Camera.main, backgroundFitter.Backgrounds);
        backgroundFitter.Fit();
    }

    void Update()
    {
        if (isStaticMenuCamera)
            return;

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
        if (isStaticMenuCamera)
            return;

        backgroundParallax.Update(Camera.main, backgroundMovementFraction);
        backgroundFitter.Fit();
    }

    void zoom(float increment)
    {
        float newSize = Mathf.Clamp(Camera.main.orthographicSize - increment, zoomMin, zoomMax);
        Camera.main.orthographicSize = newSize;
        Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
    }

    Vector3 ClampCamera(Vector3 targetPosition)
    {
        float cameraHalfHeight = Camera.main.orthographicSize;

        float boundsHeight = maxBounds.y - minBounds.y;
        float boundsCenterY = (minBounds.y + maxBounds.y) * 0.5f;

        float minY = minBounds.y + cameraHalfHeight;
        float maxY = maxBounds.y - cameraHalfHeight;

        float clampedX;
        if (mainForm != null)
        {
            mainForm.GetWorldCorners(formCorners);
            float formWidth = Mathf.Abs(formCorners[2].x - formCorners[0].x);
            float maximumOffset = formWidth * formHorizontalTravelFraction;
            clampedX = Mathf.Clamp(targetPosition.x,
                initialCameraX - maximumOffset,
                initialCameraX + maximumOffset);
        }
        else
        {
            clampedX = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
        }

        float clampedY = cameraHalfHeight * 2f >= boundsHeight
            ? boundsCenterY
            : Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPosition.z);
    }

    private RectTransform FindMainForm()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.gameObject.scene == gameObject.scene &&
                canvas.isRootCanvas &&
                canvas.renderMode == RenderMode.WorldSpace)
                return canvas.transform as RectTransform;
        }

        return null;
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
            if (scrollView != null && scrollView.activeSelf)
            {
                return true;
            }
        }
        return false;
    }
}
