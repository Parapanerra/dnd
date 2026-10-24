using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ButtonMotion-style feedback for the selected image of an image dropdown.
[RequireComponent(typeof(Dropdown))]
[DisallowMultipleComponent]
public sealed class DropdownCaptionMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private const float HoverScale = 1.06f;
    private const float PressScale = 0.93f;
    private const float ResponseSpeed = 18f;
    private const float ClickHold = 0.15f;

    private Dropdown dropdown;
    private RectTransform caption;
    private Vector3 normalScale;
    private ColorBlock normalColors;
    private bool hovering;
    private bool pointerDown;
    private float clickUntil;

    private void Awake()
    {
        dropdown = GetComponent<Dropdown>();
    }

    private void OnEnable()
    {
        if (dropdown == null)
            dropdown = GetComponent<Dropdown>();

        caption = dropdown.captionImage != null
            ? dropdown.captionImage.rectTransform : null;
        if (caption != null)
            normalScale = caption.localScale;

        normalColors = dropdown.colors;
        if (!Application.isMobilePlatform)
        {
            ColorBlock colors = normalColors;
            colors.highlightedColor = Color.Lerp(normalColors.highlightedColor,
                new Color(1f, 0.82f, 0.60f, normalColors.highlightedColor.a), 0.45f);
            dropdown.colors = colors;
        }
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.colors = normalColors;
        if (caption != null)
            caption.localScale = normalScale;
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
        if (dropdown.interactable)
            pointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (dropdown.interactable)
            clickUntil = Time.unscaledTime + ClickHold;
    }

    private void Update()
    {
        if (caption == null)
            return;

        float target = 1f;
        if (dropdown.interactable)
        {
            if (pointerDown || Time.unscaledTime < clickUntil)
                target = PressScale;
            else if (hovering && !Application.isMobilePlatform)
                target = HoverScale;
        }

        float blend = 1f - Mathf.Exp(-ResponseSpeed * Time.unscaledDeltaTime);
        caption.localScale = Vector3.Lerp(caption.localScale, normalScale * target, blend);
    }
}
