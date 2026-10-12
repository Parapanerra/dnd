using TMPro;
using UnityEngine;

public class LocalizedTmpText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TMP_Text text;
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Apply();
    }

    public void Apply(RuntimeLocalization service = null)
    {
        if (service != null)
            localization = service;
        if (localization == null)
            localization = new RuntimeManagerResolver().Localization();
        if (text == null)
            text = GetComponent<TMP_Text>();

        if (text == null)
            return;

        sourceText = LocalizedTextSource.Resolve(sourceText, text.text, localization);
        text.text = localization.Translate(sourceText);
    }

}
