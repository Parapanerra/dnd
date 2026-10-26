using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Taruck.CharacterCreation
{
    // Small view factory used only by the experimental wizard scene.
    public sealed class CreationWizardUi
    {
        public static readonly Color Gold = new Color(.91f, .67f, .32f);
        public static readonly Color Ink = new Color(.045f, .045f, .075f, .98f);
        private readonly TMP_FontAsset font;
        private readonly Sprite buttonSprite;
        private readonly Sprite inputSprite;
        private readonly Sprite dropdownArrowSprite;
        private readonly Sprite dropdownTemplateSprite;
        private readonly Sprite scrollTrackSprite;
        private readonly Sprite scrollHandleSprite;
        private readonly Func<string, string> translate;
        public CreationWizardUi(TMP_FontAsset font, Sprite buttonSprite, Sprite inputSprite = null, Sprite dropdownArrowSprite = null, Sprite dropdownTemplateSprite = null, Sprite scrollTrackSprite = null, Sprite scrollHandleSprite = null, Func<string, string> translate = null)
        { this.font = font; this.buttonSprite = buttonSprite; this.inputSprite = inputSprite; this.dropdownArrowSprite = dropdownArrowSprite; this.dropdownTemplateSprite = dropdownTemplateSprite; this.scrollTrackSprite = scrollTrackSprite; this.scrollHandleSprite = scrollHandleSprite; this.translate = translate ?? (value => value); }

        public RectTransform Rect(string name, Transform parent)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false);
            return r;
        }
        public static void Fill(RectTransform r, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(left, bottom); r.offsetMax = new Vector2(-right, -top);
        }
        public static void Height(RectTransform r, float h)
        {
            var e = r.gameObject.AddComponent<UnityEngine.UI.LayoutElement>(); e.minHeight = h; e.preferredHeight = h;
        }
        public RectTransform Panel(string name, Transform parent, Color color)
        {
            var r = Rect(name, parent); var i = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            i.color = color; i.raycastTarget = false; return r;
        }
        public TextMeshProUGUI Text(Transform parent, string value, float size = 23, Color? color = null, float height = 42)
        {
            var r = Rect("Text", parent);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = font; t.text = translate(value); t.fontSize = size; t.color = color ?? new Color(.89f, .86f, .81f);
            t.raycastTarget = false; t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            Height(r, height); return t;
        }
        public RectTransform Column(string name, Transform parent, float spacing = 12)
        {
            var r = Rect(name, parent); var layout = r.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.spacing = spacing; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            return r;
        }
        public RectTransform Row(string name, Transform parent, float h = 72)
        {
            var r = Rect(name, parent); Height(r, h);
            r.GetComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
            var layout = r.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            layout.spacing = 12; layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = true;
            return r;
        }
        public UnityEngine.UI.Button Button(Transform parent, string label, UnityAction action, bool selected = false, float height = 66, bool showCheck = false)
        {
            var r = Rect(label.Replace('\n', ' '), parent); Height(r, height);
            r.GetComponent<UnityEngine.UI.LayoutElement>().preferredWidth = Mathf.Min(520, height * 4.5f);
            var image = r.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = buttonSprite;
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 4;
            image.color = Color.white;
            var b = r.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = image;
            var colors = b.colors; colors.highlightedColor = selected ? new Color(.87f, .80f, .70f) : new Color(1f, .91f, .73f);
            colors.pressedColor = new Color(.62f, .56f, .49f);
            colors.normalColor = selected ? new Color(.70f, .64f, .57f) : Color.white;
            colors.selectedColor = colors.normalColor;
            colors.fadeDuration = 0;
            b.colors = colors;
            b.onClick.AddListener(action);
            var text = Text(r, label, 21, selected ? Gold : new Color(.87f, .83f, .76f));
            Fill(text.rectTransform, 18, 9, showCheck && selected ? 37 : 18, 9); text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 21;
            if (showCheck && selected && dropdownArrowSprite != null)
            {
                var check = Rect("SelectedCheck", r);
                check.anchorMin = check.anchorMax = new Vector2(1, .5f);
                check.pivot = new Vector2(1, .5f);
                check.anchoredPosition = new Vector2(-8, 0);
                check.sizeDelta = new Vector2(22, 21);
                var checkImage = check.gameObject.AddComponent<UnityEngine.UI.Image>();
                checkImage.sprite = dropdownArrowSprite;
                checkImage.preserveAspect = true;
                checkImage.raycastTarget = false;
                checkImage.pixelsPerUnitMultiplier = 1;
            }
            return b;
        }
        public TMP_Dropdown Dropdown(Transform parent, string name, IList<string> choices, int selected, Action<int> changed, float width = 300, float height = 48)
        {
            Transform host = parent;
            if (parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null)
            {
                var row = Row("DropdownRow", parent, height);
                var horizontal = row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                horizontal.childForceExpandWidth = false;
                host = row;
                var spacer = Rect("Spacer", row);
                var spacerSize = spacer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                spacerSize.flexibleWidth = 1; spacerSize.preferredWidth = 0;
            }
            var root = Rect(name, host); Height(root, height);
            if (host != parent) root.SetAsFirstSibling();
            var sizing = root.GetComponent<UnityEngine.UI.LayoutElement>();
            sizing.preferredWidth = width; sizing.flexibleWidth = 0;
            var image = root.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.sprite = buttonSprite; image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 3;
            var dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = image;
            root.gameObject.AddComponent<CreationDropdownMotion>();
            bool compactDropdown = width <= 120;
            var caption = Text(root, "", compactDropdown ? 18 : 19, new Color(.787f, .475f, 0f));
            Fill(caption.rectTransform, compactDropdown ? 8 : 12, 5, compactDropdown ? 24 : 32, 5);
            caption.alignment = TextAlignmentOptions.MidlineLeft;
            caption.enableAutoSizing = true; caption.fontSizeMin = compactDropdown ? 15 : 16;
            var arrow = Rect("Arrow", root);
            arrow.anchorMin = arrow.anchorMax = new Vector2(1, .5f);
            arrow.pivot = new Vector2(1, .5f);
            arrow.anchoredPosition = new Vector2(compactDropdown ? -5 : -7, 0);
            arrow.sizeDelta = compactDropdown ? new Vector2(17, 16) : new Vector2(23, 20);
            var arrowImage = arrow.gameObject.AddComponent<UnityEngine.UI.Image>();
            arrowImage.sprite = dropdownArrowSprite; arrowImage.preserveAspect = true; arrowImage.raycastTarget = false;
            arrowImage.pixelsPerUnitMultiplier = 1;

            var template = Panel("Template", root, Ink);
            template.anchorMin = new Vector2(0, 0); template.anchorMax = new Vector2(1, 0);
            template.pivot = new Vector2(.5f, 1);
            template.anchoredPosition = Vector2.zero;
            bool needsScroll = choices.Count > 6;
            template.sizeDelta = new Vector2(0, Mathf.Min(236, choices.Count * 38 + 8));
            var templateImage = template.GetComponent<UnityEngine.UI.Image>();
            templateImage.sprite = dropdownTemplateSprite != null ? dropdownTemplateSprite : buttonSprite;
            templateImage.type = UnityEngine.UI.Image.Type.Sliced;
            templateImage.pixelsPerUnitMultiplier = 3;
            templateImage.color = Color.white;
            var scroll = template.gameObject.AddComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 70;
            var viewport = Rect("Viewport", template); Fill(viewport, 4, 4, needsScroll ? 22 : 4, 4);
            var viewportImage = viewport.gameObject.AddComponent<UnityEngine.UI.Image>();
            viewportImage.sprite = buttonSprite; viewportImage.type = UnityEngine.UI.Image.Type.Sliced;
            viewportImage.pixelsPerUnitMultiplier = 1; viewportImage.raycastTarget = false;
            viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
            layout.childControlWidth = true; layout.childControlHeight = true;
            layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>().verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.viewport = viewport;
            var item = Panel("Item", content, Color.white); Height(item, 38);
            var itemImage = item.GetComponent<UnityEngine.UI.Image>();
            itemImage.sprite = buttonSprite; itemImage.type = UnityEngine.UI.Image.Type.Sliced;
            itemImage.pixelsPerUnitMultiplier = 3;
            itemImage.raycastTarget = true;
            var toggle = item.gameObject.AddComponent<UnityEngine.UI.Toggle>();
            toggle.targetGraphic = itemImage;
            item.gameObject.AddComponent<DropdownItemMotion>();
            var check = Rect("Item Checkmark", item);
            check.anchorMin = check.anchorMax = new Vector2(1, .5f); check.pivot = new Vector2(1, .5f);
            check.anchoredPosition = new Vector2(-7, 0); check.sizeDelta = new Vector2(20, 18);
            var checkImage = check.gameObject.AddComponent<UnityEngine.UI.Image>();
            checkImage.sprite = dropdownArrowSprite; checkImage.preserveAspect = true; checkImage.raycastTarget = false;
            checkImage.pixelsPerUnitMultiplier = 1;
            toggle.graphic = checkImage;
            var label = Text(item, "", compactDropdown ? 16 : 17, new Color(.787f, .475f, 0f));
            Fill(label.rectTransform, compactDropdown ? 8 : 12, 3, compactDropdown ? 24 : 32, 3);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.enableAutoSizing = true; label.fontSizeMin = 14;
            if (needsScroll)
            {
                var track = Rect("Scrollbar", template);
                track.anchorMin = new Vector2(1, 0); track.anchorMax = Vector2.one;
                track.offsetMin = new Vector2(-19, 4); track.offsetMax = new Vector2(-3, -4);
                var trackImage = track.gameObject.AddComponent<UnityEngine.UI.Image>();
                trackImage.sprite = scrollTrackSprite; trackImage.type = UnityEngine.UI.Image.Type.Sliced;
                trackImage.pixelsPerUnitMultiplier = 4;
                var sliding = Rect("Sliding Area", track); Fill(sliding, 2, 3, 2, 3);
                var handle = Rect("Handle", sliding); Fill(handle);
                var handleImage = handle.gameObject.AddComponent<UnityEngine.UI.Image>();
                handleImage.sprite = scrollHandleSprite; handleImage.type = UnityEngine.UI.Image.Type.Sliced;
                handleImage.pixelsPerUnitMultiplier = 1;
                var scrollbar = track.gameObject.AddComponent<UnityEngine.UI.Scrollbar>();
                scrollbar.targetGraphic = handleImage; scrollbar.handleRect = handle;
                scrollbar.direction = UnityEngine.UI.Scrollbar.Direction.BottomToTop;
                scroll.verticalScrollbar = scrollbar;
                scroll.verticalScrollbarVisibility = UnityEngine.UI.ScrollRect.ScrollbarVisibility.Permanent;
            }
            dropdown.template = template; dropdown.captionText = caption; dropdown.itemText = label;
            dropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (string choice in choices) options.Add(new TMP_Dropdown.OptionData(translate(choice)));
            dropdown.AddOptions(options);
            dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, choices.Count - 1));
            dropdown.RefreshShownValue();
            dropdown.onValueChanged.AddListener(value => changed(value));
            template.gameObject.SetActive(false);
            return dropdown;
        }
        public TMP_InputField Input(Transform parent, string initial, string placeholder, Action<string> changed)
        {
            Transform host = parent;
            if (parent.GetComponent<UnityEngine.UI.VerticalLayoutGroup>() != null)
            {
                var row = Row("NameInputRow", parent, 72);
                row.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().childForceExpandWidth = false;
                var spacer = Rect("Spacer", row);
                var spacerSize = spacer.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                spacerSize.flexibleWidth = 1;
                host = row;
            }
            var r = Panel("CharacterName", host, new Color(.055f, .06f, .09f)); Height(r, 72);
            if (host != parent) r.SetAsFirstSibling();
            var layout = r.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredWidth = 390; layout.flexibleWidth = 0;
            var inputImage = r.GetComponent<UnityEngine.UI.Image>();
            inputImage.raycastTarget = true;
            if (inputSprite != null) { inputImage.sprite = inputSprite; inputImage.type = UnityEngine.UI.Image.Type.Sliced; inputImage.pixelsPerUnitMultiplier = 4; inputImage.color = Color.white; }
            var viewport = Rect("TextArea", r); Fill(viewport, 20, 10, 20, 10); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Text(viewport, "", 27, new Color(1f, .604f, 0f)); Fill(text.rectTransform);
            var hint = Text(viewport, placeholder, 25, new Color(.5f, .49f, .48f)); Fill(hint.rectTransform);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            hint.alignment = TextAlignmentOptions.MidlineLeft;
            var field = r.gameObject.AddComponent<TMP_InputField>(); field.textViewport = viewport;
            field.textComponent = text; field.placeholder = hint; field.targetGraphic = r.GetComponent<UnityEngine.UI.Image>();
            field.characterLimit = 80; field.lineType = TMP_InputField.LineType.SingleLine; field.text = initial;
            field.customCaretColor = true; field.caretColor = new Color(1f, .604f, 0f); field.caretWidth = 1;
            field.caretBlinkRate = .85f;
            r.gameObject.AddComponent<InputFieldMotion>();
            var activator = r.gameObject.AddComponent<DoubleClickInputFieldActivator>();
            activator.Configure(field, false);
            field.caretWidth = 1;
            if (!Application.isMobilePlatform)
            {
                var history = r.gameObject.AddComponent<DesktopInputHistory>();
                history.Configure(field);
            }
            field.onSelect.AddListener(_ => hint.gameObject.SetActive(false));
            field.onDeselect.AddListener(_ => hint.gameObject.SetActive(string.IsNullOrEmpty(field.text)));
            field.onValueChanged.AddListener(value => changed(value)); return field;
        }
        public void Clear(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject; child.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child); else UnityEngine.Object.DestroyImmediate(child);
            }
        }
    }

    // Motion for the TMP dropdown caption; list items use the existing DropdownItemMotion.
    internal sealed class CreationDropdownMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private TMP_Dropdown dropdown;
        private Vector3 normalScale;
        private bool hovering;
        private bool pressed;

        private void Awake() { dropdown = GetComponent<TMP_Dropdown>(); normalScale = transform.localScale; }
        private void OnDisable() { transform.localScale = normalScale; hovering = pressed = false; }
        public void OnPointerEnter(PointerEventData _) { if (!Application.isMobilePlatform) hovering = true; }
        public void OnPointerExit(PointerEventData _) { hovering = pressed = false; }
        public void OnPointerDown(PointerEventData _) { if (dropdown.IsInteractable()) pressed = true; }
        public void OnPointerUp(PointerEventData _) { pressed = false; }
        private void Update()
        {
            float scale = pressed ? .96f : hovering ? 1.04f : 1f;
            transform.localScale = Vector3.Lerp(transform.localScale, normalScale * scale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        }
    }
}
