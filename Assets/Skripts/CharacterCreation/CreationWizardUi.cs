using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

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
        public CreationWizardUi(TMP_FontAsset font, Sprite buttonSprite, Sprite inputSprite = null)
        { this.font = font; this.buttonSprite = buttonSprite; this.inputSprite = inputSprite; }

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
            t.font = font; t.text = value; t.fontSize = size; t.color = color ?? new Color(.89f, .86f, .81f);
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
        public UnityEngine.UI.Button Button(Transform parent, string label, UnityAction action, bool selected = false, float height = 66)
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
            colors.normalColor = selected ? new Color(.78f, .72f, .64f) : Color.white;
            colors.selectedColor = colors.normalColor;
            b.colors = colors;
            b.onClick.AddListener(action);
            var text = Text(r, label, 21, selected ? Gold : new Color(.87f, .83f, .76f));
            Fill(text.rectTransform, 18, 9, 18, 9); text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true; text.fontSizeMin = 15; text.fontSizeMax = 21;
            return b;
        }
        public TMP_InputField Input(Transform parent, string initial, string placeholder, Action<string> changed)
        {
            var r = Panel("CharacterName", parent, new Color(.055f, .06f, .09f)); Height(r, 72);
            var layout = r.GetComponent<UnityEngine.UI.LayoutElement>();
            layout.preferredWidth = 520; layout.flexibleWidth = 1;
            var inputImage = r.GetComponent<UnityEngine.UI.Image>();
            inputImage.raycastTarget = true;
            if (inputSprite != null) { inputImage.sprite = inputSprite; inputImage.type = UnityEngine.UI.Image.Type.Sliced; inputImage.pixelsPerUnitMultiplier = 4; inputImage.color = Color.white; }
            var border = r.gameObject.AddComponent<UnityEngine.UI.Outline>(); border.effectColor = new Color(.55f, .39f, .2f); border.effectDistance = new Vector2(1, -1);
            var viewport = Rect("TextArea", r); Fill(viewport, 20, 10, 20, 10); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text = Text(viewport, "", 27); Fill(text.rectTransform);
            var hint = Text(viewport, placeholder, 25, new Color(.5f, .49f, .48f)); Fill(hint.rectTransform);
            var field = r.gameObject.AddComponent<TMP_InputField>(); field.textViewport = viewport;
            field.textComponent = text; field.placeholder = hint; field.targetGraphic = r.GetComponent<UnityEngine.UI.Image>();
            field.characterLimit = 80; field.lineType = TMP_InputField.LineType.SingleLine; field.text = initial;
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
}
