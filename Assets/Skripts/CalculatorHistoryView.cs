using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CalculatorHistoryView : MonoBehaviour
{
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject panel;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform content;
    [SerializeField] private Font font;

    private void Awake()
    {
        if (toggleButton == null)
            toggleButton = transform.Find("historyBatton")?.GetComponent<Button>();
        if (panel == null)
            panel = transform.Find("historyPanel")?.gameObject;

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);

        if (content != null)
        {
            foreach (CalculatorHistory.Entry entry in CalculatorHistory.Entries)
                Append(entry, false);
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnEnable()
    {
        CalculatorHistory.Added += OnEntryAdded;
    }

    private void OnDisable()
    {
        CalculatorHistory.Added -= OnEntryAdded;
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(Toggle);
    }

    private void Toggle()
    {
        if (panel == null)
            return;

        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf && scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void OnEntryAdded(CalculatorHistory.Entry entry)
    {
        Append(entry, true);
    }

    private void Append(CalculatorHistory.Entry entry, bool scrollToLatest)
    {
        if (content == null)
            return;

        var row = new GameObject("History entry", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        row.transform.SetParent(content, false);
        var rect = (RectTransform)row.transform;
        rect.localScale = Vector3.one;
        var label = row.GetComponent<Text>();
        label.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 16;
        label.color = entry.Color;
        label.supportRichText = false;
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        label.text = entry.Text;
        var layout = row.GetComponent<LayoutElement>();
        layout.minHeight = 22f;

        if (scrollToLatest && scrollRect != null && panel != null && panel.activeSelf)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
