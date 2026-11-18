using UnityEngine;
using UnityEngine.UI;

// Keep the saved InputField and grow its scroll range only when real text overflows.
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class NoteScrollInput : MonoBehaviour
{
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private InputField input;
    [SerializeField, Min(1)] private int characterLimit = 3000;
    private string previousText;
    private float previousWidth, previousHeight;
    private int previousCaret = -1;

    private void OnEnable()
    {
        if (scroll == null) scroll = GetComponent<ScrollRect>();
        if (input == null) input = GetComponentInChildren<InputField>(true);
        // Upgrade older serialized note readers as well as newly added ones.
        characterLimit = Mathf.Max(3000, characterLimit);
        previousText = null;
        if (input != null) input.characterLimit = characterLimit;
    }

    public void Configure(ScrollRect view, InputField field)
    {
        scroll = view;
        input = field;
        input.characterLimit = characterLimit;
        previousText = null;
    }

    private void LateUpdate()
    {
        if (scroll == null || input == null || input.textComponent == null) return;
        bool editing = input.isActiveAndEnabled && input.isFocused;
        // The app's double-click activator owns edit mode. While it is unlocked,
        // the InputField handles drags as selection; the parent must not scroll.
        if (scroll.enabled == editing)
        {
            scroll.StopMovement();
            scroll.enabled = !editing;
        }
        if (scroll.verticalScrollbar != null)
            scroll.verticalScrollbar.interactable = !editing;
        float width = input.textComponent.rectTransform.rect.width;
        float height = scroll.viewport.rect.height;
        if (width <= 0 || height <= 0) return;
        bool changed = previousText != input.text || !Mathf.Approximately(width, previousWidth) ||
            !Mathf.Approximately(height, previousHeight);
        var text = input.textComponent;
        if (changed)
        {
            var settings = text.GetGenerationSettings(new Vector2(width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float required = text.cachedTextGeneratorForLayout.GetPreferredHeight(input.text, settings) / text.pixelsPerUnit + 12;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height, required));
            // InputField caches the visible substring using its previous height.
            input.ForceLabelUpdate();
            previousText = input.text;
            previousWidth = width;
            previousHeight = height;
        }
        // Follow editing, but let the wheel/handle browse freely while the caret stays put.
        if (editing && (changed || previousCaret != input.caretPosition))
        {
            int caret = Mathf.Clamp(input.caretPosition, 0, input.text.Length);
            var settings = text.GetGenerationSettings(new Vector2(width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float bottom = text.cachedTextGeneratorForLayout.GetPreferredHeight(input.text.Substring(0, caret), settings) / text.pixelsPerUnit + 6;
            float offset = scroll.content.anchoredPosition.y;
            float lineHeight = text.fontSize * text.lineSpacing + 4;
            if (bottom > offset + height) offset = bottom - height;
            else if (bottom - lineHeight < offset) offset = bottom - lineHeight;
            scroll.StopMovement();
            scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, scroll.content.rect.height - height));
        }
        previousCaret = editing ? input.caretPosition : -1;
        // Resizing/deleting text must also remove any old offset into empty space.
        float maximumOffset = Mathf.Max(0, scroll.content.rect.height - height);
        bool overflow = maximumOffset > .5f;
        scroll.vertical = overflow;
        if (!overflow) scroll.StopMovement();
        if (scroll.verticalScrollbar != null)
        {
            scroll.verticalScrollbar.interactable = !editing && overflow;
            if (!overflow)
            {
                scroll.verticalScrollbar.SetValueWithoutNotify(1);
                scroll.verticalScrollbar.size = 1;
            }
        }
        Vector2 position = scroll.content.anchoredPosition;
        scroll.content.anchoredPosition = new Vector2(0, Mathf.Clamp(position.y, 0, maximumOffset));
    }
}
