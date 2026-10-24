using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Adds visual feedback without changing the button's click or navigation behavior.
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class ButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    private const float HoverScale = 1.06f;
    private const float PressScale = 0.93f;
    private const float ResponseSpeed = 18f;
    private const float ClickHold = 0.15f;

    private Button button;
    private Vector3 normalScale;
    private ColorBlock normalColors;
    private bool hovering;
    private bool pointerDown;
    private float clickUntil;
    private ButtonMotion hoveredRowParent;
    private int hoveredChildButtons;

    internal float LayoutScaleFactor => Mathf.Approximately(normalScale.x, 0f)
        ? 1f : transform.localScale.x / normalScale.x;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (button == null)
            button = GetComponent<Button>();

        normalScale = transform.localScale;
        normalColors = button.colors;
        if (!Application.isMobilePlatform)
        {
            ColorBlock colors = normalColors;
            colors.highlightedColor = Color.Lerp(normalColors.highlightedColor,
                new Color(1f, 0.82f, 0.60f, normalColors.highlightedColor.a), 0.45f);
            button.colors = colors;
        }

        button.onClick.AddListener(OnClicked);
    }

    private void OnDisable()
    {
        ReleaseRowParent();
        button.onClick.RemoveListener(OnClicked);
        button.colors = normalColors;
        transform.localScale = normalScale;
        hovering = false;
        pointerDown = false;
        clickUntil = 0f;
        hoveredChildButtons = 0;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!Application.isMobilePlatform)
        {
            hovering = true;
            hoveredRowParent = FindCharacterMenuRowParent();
            if (hoveredRowParent != null)
                hoveredRowParent.hoveredChildButtons++;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseRowParent();
        hovering = false;
        pointerDown = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable)
            pointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerDown = false;
    }

    private void OnClicked()
    {
        clickUntil = Time.unscaledTime + ClickHold;
    }

    private void Update()
    {
        float target = 1f;
        if (button.interactable && hoveredChildButtons == 0)
        {
            if (pointerDown || Time.unscaledTime < clickUntil)
                target = PressScale;
            else if (hovering && !Application.isMobilePlatform)
                target = HoverScale;
        }

        float blend = 1f - Mathf.Exp(-ResponseSpeed * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, normalScale * target, blend);
    }

    private ButtonMotion FindCharacterMenuRowParent()
    {
        for (Transform parent = transform.parent; parent != null; parent = parent.parent)
        {
            ButtonMotion motion = parent.GetComponent<ButtonMotion>();
            if (motion == null)
                continue;

            string rowName = parent.name;
            if (rowName != "kartaPerson (1)" && !rowName.StartsWith("CharacterMenuClone_Row_"))
                continue;

            ScrollRect scroll = parent.GetComponentInParent<ScrollRect>(true);
            if (scroll != null && scroll.name == "menukart")
                return motion;
        }

        return null;
    }

    private void ReleaseRowParent()
    {
        if (hoveredRowParent != null)
            hoveredRowParent.hoveredChildButtons = Mathf.Max(0, hoveredRowParent.hoveredChildButtons - 1);
        hoveredRowParent = null;
    }
}
