using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class ManualLocalizedText : MonoBehaviour
{
    [TextArea(1, 4)]
    [FormerlySerializedAs("sourceText")]
    [SerializeField] private string ukrainianText;

    [Header("Translations")]
    [TextArea(1, 4)]
    [SerializeField] private string englishText;

    [TextArea(1, 4)]
    [SerializeField] private string russianText;

    [Header("Setup")]
    [SerializeField] private bool useCurrentTextAsSource = true;

    private Text uiText;
    private TMP_Text tmpText;
    private TextMesh textMesh;

    private void Awake()
    {
        CacheTextComponent();
        CaptureSourceIfNeeded();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply()
    {
        CacheTextComponent();
        CaptureSourceIfNeeded();

        if (string.IsNullOrWhiteSpace(ukrainianText))
            return;

        string translated = GetTranslatedText();
        if (uiText != null)
            uiText.text = translated;
        else if (tmpText != null)
            tmpText.text = translated;
        else if (textMesh != null)
            textMesh.text = translated;
    }

    private string GetTranslatedText()
    {
        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        if (localization.CurrentLanguage == AppLanguage.English && !string.IsNullOrWhiteSpace(englishText))
            return englishText;

        if (localization.CurrentLanguage == AppLanguage.Russian && !string.IsNullOrWhiteSpace(russianText))
            return russianText;

        return localization.Translate(ukrainianText);
    }

    private void CacheTextComponent()
    {
        if (uiText == null)
            uiText = GetComponent<Text>();
        if (uiText == null)
            uiText = GetComponentInChildren<Text>(true);

        if (tmpText == null)
            tmpText = GetComponent<TMP_Text>();
        if (tmpText == null)
            tmpText = GetComponentInChildren<TMP_Text>(true);

        if (textMesh == null)
            textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>(true);
    }

    private void CaptureSourceIfNeeded()
    {
        if (!useCurrentTextAsSource || !string.IsNullOrWhiteSpace(ukrainianText))
            return;

        string currentText = GetCurrentText();
        if (!string.IsNullOrWhiteSpace(currentText))
            ukrainianText = RuntimeLocalization.EnsureExists().GetSourceText(currentText);
    }

    private string GetCurrentText()
    {
        if (uiText != null)
            return uiText.text;
        if (tmpText != null)
            return tmpText.text;
        if (textMesh != null)
            return textMesh.text;

        return "";
    }

    private void OnValidate()
    {
        CacheTextComponent();
        if (useCurrentTextAsSource && string.IsNullOrWhiteSpace(ukrainianText))
        {
            string currentText = GetCurrentText();
            if (!string.IsNullOrWhiteSpace(currentText))
                ukrainianText = currentText;
        }
    }
}
