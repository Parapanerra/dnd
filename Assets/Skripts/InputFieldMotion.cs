using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Button-like feedback for input fields without changing their RectTransform layout.
[DisallowMultipleComponent]
public sealed class InputFieldMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private const float FadeSeconds = 0.16f;
    private const float HoverScale = 1.04f;
    private const float PressScale = 0.96f;
    private const float ResponseSpeed = 18f;
    private const float ClickHold = 0.15f;

    private Selectable field;
    private ColorBlock originalColors;
    private Vector3 normalScale;
    private bool hovering;
    private bool pointerDown;
    private float clickUntil;

    private void OnEnable()
    {
        field = GetComponent<InputField>();
        if (field == null)
            field = GetComponent<TMPro.TMP_InputField>();
        if (field == null)
            return;

        normalScale = transform.localScale;
        originalColors = field.colors;
        Color baseColor = originalColors.normalColor;
        ColorBlock colors = originalColors;
        colors.highlightedColor = Color.Lerp(baseColor,
            new Color(1f, 0.78f, 0.52f, baseColor.a), 0.46f);
        colors.pressedColor = Color.Lerp(baseColor,
            new Color(1f, 0.56f, 0.32f, baseColor.a), 0.65f);
        colors.selectedColor = Color.Lerp(baseColor,
            new Color(1f, 0.82f, 0.61f, baseColor.a), 0.40f);
        colors.fadeDuration = FadeSeconds;
        field.colors = colors;
    }

    private void OnDisable()
    {
        if (field != null)
            field.colors = originalColors;
        transform.localScale = normalScale;
        hovering = false;
        pointerDown = false;
        clickUntil = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Application.isMobilePlatform)
            hovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        pointerDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (field != null && field.IsInteractable())
            pointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (field != null && field.IsInteractable())
            clickUntil = Time.unscaledTime + ClickHold;
    }

    private void Update()
    {
        if (field == null)
            return;

        float target = 1f;
        if (field.IsInteractable())
        {
            if (pointerDown || Time.unscaledTime < clickUntil)
                target = PressScale;
            else if (hovering && !Application.isMobilePlatform)
                target = HoverScale;
        }

        float blend = 1f - Mathf.Exp(-ResponseSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, normalScale * target, blend);
    }
}
