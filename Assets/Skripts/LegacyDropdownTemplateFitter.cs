using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sizes a legacy dropdown's visible list from its current option count.
public sealed class LegacyDropdownTemplateFitter : MonoBehaviour, IPointerDownHandler
{
    [SerializeField, Min(1)] private int maxVisibleItems = 5;
    [SerializeField, Min(0f)] private float extraVerticalSpace;

    private Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<Dropdown>();
    }

    private void OnEnable()
    {
        Fit();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Fit();
    }

    private void Fit()
    {
        if (dropdown == null)
            dropdown = GetComponent<Dropdown>();
        if (dropdown == null || dropdown.template == null)
            return;

        RectTransform template = dropdown.template;
        RectTransform viewport = template.Find("Viewport") as RectTransform;
        RectTransform item = viewport != null ? viewport.Find("Content/Item") as RectTransform : null;
        if (viewport == null || item == null)
            return;

        int visibleItems = Mathf.Min(Mathf.Max(dropdown.options.Count, 1), maxVisibleItems);
        float frameHeight = template.rect.height - viewport.rect.height;
        // Unity adds a small inset to the generated Content. Give fully visible
        // lists room for it so AutoHide does not show an unusable scrollbar.
        float contentInset = dropdown.options.Count <= maxVisibleItems ? 3f : 0f;
        float requiredHeight = visibleItems * item.rect.height + frameHeight + extraVerticalSpace + contentInset;
        template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, requiredHeight);
    }
}
