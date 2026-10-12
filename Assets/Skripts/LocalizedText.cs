using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private Text text;
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<Text>();
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
            text = GetComponent<Text>();

        if (text == null)
            return;

        sourceText = LocalizedTextSource.Resolve(sourceText, text.text, localization);
        text.text = localization.Translate(sourceText);
    }

}
