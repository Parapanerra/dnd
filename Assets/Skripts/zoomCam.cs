using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class zoomCam : MonoBehaviour
{
    Vector3 touchStart;
    public float zoomMin = 1;
    public float zoomMax = 8;
    public Vector2 minBounds;
    public Vector2 maxBounds;
    public List<GameObject> scrollViews = new List<GameObject>(); // Список всех скролл вью в сцене

    void Start()
    {
        // Убедиться, что камера находится внутри границ при запуске
        Camera.main.transform.position = ClampCamera(Camera.main.transform.position);
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

            zoom(difference * 0.01f);
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

        float minX = minBounds.x + cameraHalfWidth;
        float maxX = maxBounds.x - cameraHalfWidth;
        float minY = minBounds.y + cameraHalfHeight;
        float maxY = maxBounds.y - cameraHalfHeight;

        float clampedX = Mathf.Clamp(targetPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(targetPosition.y, minY, maxY);

        return new Vector3(clampedX, clampedY, targetPosition.z);
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
