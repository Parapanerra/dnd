using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Applies the selected language to scene UI while preserving editable user text.
internal sealed class SceneLocalizationApplier
{
    private readonly RuntimeLocalization localization;

    internal SceneLocalizationApplier(RuntimeLocalization localization)
    {
        this.localization = localization;
    }

    internal void Apply()
    {
        foreach (ManualLocalizedText text in UnityEngine.Object.FindObjectsByType<ManualLocalizedText>(FindObjectsInactive.Include))
        {
            if (IsSceneObject(text != null ? text.gameObject : null))
                text.Apply(localization);
        }

        foreach (Text text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null) || ShouldSkip(text))
                continue;

            LocalizedText localizedText = text.GetComponent<LocalizedText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedText>();
            localizedText.Apply(localization);
        }

        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null) || ShouldSkip(text))
                continue;

            LocalizedTmpText localizedText = text.GetComponent<LocalizedTmpText>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTmpText>();
            localizedText.Apply(localization);
        }

        foreach (TextMesh text in UnityEngine.Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include))
        {
            if (!IsSceneObject(text != null ? text.gameObject : null) || ShouldSkip(text))
                continue;

            LocalizedTextMesh localizedText = text.GetComponent<LocalizedTextMesh>();
            if (localizedText == null)
                localizedText = text.gameObject.AddComponent<LocalizedTextMesh>();
            localizedText.Apply(localization);
        }

        foreach (Dropdown dropdown in UnityEngine.Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include))
        {
            if (IsSceneObject(dropdown != null ? dropdown.gameObject : null))
                ApplyDropdownOptions(dropdown);
        }

        foreach (TMP_Dropdown dropdown in UnityEngine.Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include))
        {
            if (IsSceneObject(dropdown != null ? dropdown.gameObject : null))
                ApplyDropdownOptions(dropdown);
        }

        foreach (CalculatorManager calculator in UnityEngine.Object.FindObjectsByType<CalculatorManager>(FindObjectsInactive.Include))
        {
            if (IsSceneObject(calculator != null ? calculator.gameObject : null))
                calculator.RefreshLocalization();
        }

        foreach (InventoryItemCell inventoryCell in UnityEngine.Object.FindObjectsByType<InventoryItemCell>(FindObjectsInactive.Include))
        {
            if (IsSceneObject(inventoryCell != null ? inventoryCell.gameObject : null))
                inventoryCell.RefreshLocalization();
        }

        if (load_scenes.GetVisualSceneName(SceneManager.GetActiveScene().name) == AppConfig.Scenes.WildShape)
        {
            WildShapeTitleUpdater updater = UnityEngine.Object.FindAnyObjectByType<WildShapeTitleUpdater>();
            if (updater != null)
                updater.Apply();
        }
    }

    private void ApplyDropdownOptions(Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null)
            return;

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string translated = localization.Translate(dropdown.options[i].text);
            if (translated != dropdown.options[i].text)
                dropdown.options[i].text = translated;
        }

        dropdown.RefreshShownValue();
    }

    private void ApplyDropdownOptions(TMP_Dropdown dropdown)
    {
        if (dropdown == null || dropdown.options == null)
            return;

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            string translated = localization.Translate(dropdown.options[i].text);
            if (translated != dropdown.options[i].text)
                dropdown.options[i].text = translated;
        }

        dropdown.RefreshShownValue();
    }

    private static bool IsSceneObject(GameObject gameObject)
    {
        return gameObject != null && gameObject.scene.IsValid() && gameObject.scene.isLoaded;
    }

    private bool ShouldSkip(Text text)
    {
        if (text == null || text.GetComponentInParent<LocalizedIgnore>(true) != null ||
            text.GetComponentInParent<SimpleFileBrowser.FileBrowser>(true) != null ||
            text.GetComponentInParent<Dropdown>(true) != null)
            return true;

        if (IsEditableInputText(text))
            return true;

        return IsCharacterMenuLabel(text.transform);
    }

    private bool ShouldSkip(TMP_Text text)
    {
        if (text == null || text.GetComponentInParent<LocalizedIgnore>(true) != null ||
            text.GetComponentInParent<SimpleFileBrowser.FileBrowser>(true) != null ||
            text.GetComponentInParent<TMP_Dropdown>(true) != null)
            return true;

        if (IsEditableInputText(text))
            return true;

        return IsCharacterMenuLabel(text.transform);
    }

    private static bool ShouldSkip(TextMesh text)
    {
        return text == null || text.GetComponentInParent<LocalizedIgnore>(true) != null ||
            IsCharacterMenuLabel(text.transform);
    }

    private static bool IsCharacterMenuLabel(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.name == "CharacterButton" || current.name.StartsWith("CharacterButton_") ||
                current.name == "DeleteButton" || current.name == "LanguageButtons")
                return true;
        }

        return false;
    }

    private static bool IsEditableInputText(Text text)
    {
        InputField inputField = text != null ? text.GetComponentInParent<InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

    private static bool IsEditableInputText(TMP_Text text)
    {
        TMP_InputField inputField = text != null ? text.GetComponentInParent<TMP_InputField>(true) : null;
        return inputField != null && inputField.textComponent == text;
    }

}
