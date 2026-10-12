using UnityEngine;

public class LocalizedTextMesh : MonoBehaviour
{
    [SerializeField] private string sourceText;

    private TextMesh text;
    private RuntimeLocalization localization;

    private void Awake()
    {
        text = GetComponent<TextMesh>();
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
            text = GetComponent<TextMesh>();

        if (text == null)
            return;

        sourceText = LocalizedTextSource.Resolve(sourceText, text.text, localization);
        text.text = localization.Translate(sourceText);
    }

}
