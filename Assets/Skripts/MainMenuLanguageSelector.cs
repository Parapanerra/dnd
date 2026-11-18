using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns the menu's language picker and its UI state.
public class MainMenuLanguageSelector : MonoBehaviour
{
    private MainMenuManager owner;
    private RuntimeLocalization localization;
    private readonly MainMenuSceneLookup sceneLookup = new MainMenuSceneLookup();
    private Dropdown languageDropdown;
    private TMP_Dropdown languageTmpDropdown;

    internal void Initialize(MainMenuManager manager, RuntimeLocalization runtimeLocalization)
    {
        owner = manager;
        localization = runtimeLocalization;
    }

    internal void ScheduleSync()
    {
        StartCoroutine(SyncLanguageDropdownNextFrame());
    }

    private readonly struct LanguageOption
    {
        internal readonly AppLanguage Language;
        internal readonly string Label;
        internal readonly string FlagResource;

        internal LanguageOption(AppLanguage language, string label, string flagResource)
        {
            Language = language;
            Label = label;
            FlagResource = flagResource;
        }
    }

    // RU is temporarily hidden. Add it here to re-enable it.
    private readonly LanguageOption[] languageOptions =
    {
        new LanguageOption(AppLanguage.Ukrainian, "UA", "UI/LanguageFlagUA"),
        new LanguageOption(AppLanguage.English, "EN", "UI/LanguageFlagUS")
    };

    private Image ConfigureFlagImage(Graphic label, Image image)
    {
        if (image == null && label != null)
        {
            GameObject flag = new GameObject("Language Flag", typeof(RectTransform), typeof(Image));
            image = flag.GetComponent<Image>();
            image.rectTransform.SetParent(label.transform, false);
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.preserveAspect = true;
        }

        if (image != null)
            image.raycastTarget = false;

        return image;
    }

    internal void EnsureLanguageDropdown()
    {
        RemoveOldLanguageButtons();

        if (localization.CurrentLanguage == AppLanguage.Russian)
        {
            localization.SetLanguage(AppLanguage.Ukrainian);
        }

        Dropdown dropdown = sceneLookup.FindFirstDropdownInScene("localiza", "LanguageDropdown");
        TMP_Dropdown tmpDropdown = sceneLookup.FindFirstTmpDropdownInScene("localiza", "LanguageDropdown");

        if (dropdown == null && tmpDropdown == null)
        {
            Debug.LogWarning("MainMenuManager: cannot find language dropdown 'localiza'.");
            return;
        }

        Sprite[] flags = new Sprite[languageOptions.Length];
        for (int i = 0; i < flags.Length; i++)
            flags[i] = Resources.Load<Sprite>(languageOptions[i].FlagResource);

        if (dropdown != null)
            ConfigureLanguageDropdown(dropdown, flags);

        if (tmpDropdown != null)
            ConfigureLanguageDropdown(tmpDropdown, flags);
    }

    private void ConfigureLanguageDropdown(Dropdown dropdown, Sprite[] flags)
    {
        languageDropdown = dropdown;
        AddLocalizedIgnore(dropdown.gameObject);
        RepairDropdownTemplate(dropdown);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.captionImage = ConfigureFlagImage(dropdown.captionText, dropdown.captionImage);
        dropdown.itemImage = ConfigureFlagImage(dropdown.itemText, dropdown.itemImage);
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>(languageOptions.Length);
        for (int i = 0; i < languageOptions.Length; i++)
            options.Add(new Dropdown.OptionData(flags[i] == null ? languageOptions[i].Label : "", flags[i]));
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(GetCurrentLanguageIndex());
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    private void RepairDropdownTemplate(Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        if (dropdown.template == null)
            dropdown.template = CreateDropdownTemplate(dropdown);

        RectTransform template = dropdown.template;
        bool wasActive = template.gameObject.activeSelf;
        template.gameObject.SetActive(true);

        ScrollRect scrollRect = template.GetComponent<ScrollRect>();
        if (scrollRect == null)
            scrollRect = template.gameObject.AddComponent<ScrollRect>();

        Image templateImage = template.GetComponent<Image>();
        if (templateImage == null)
            templateImage = template.gameObject.AddComponent<Image>();

        templateImage.color = new Color(1f, 1f, 1f, 0f);

        RectTransform content = scrollRect != null ? scrollRect.content : null;
        if (content == null)
            content = FindOrCreateRectTransform(template, "Viewport/Content", "Content");

        Toggle itemToggle = template.GetComponentInChildren<Toggle>(true);
        if (itemToggle == null || itemToggle.transform == template)
            itemToggle = CreateDropdownTemplateItem(content);
        else if (itemToggle.transform.parent != content)
            itemToggle.transform.SetParent(content, false);

        if (dropdown.itemImage == null)
        {
            Text itemText = itemToggle.GetComponentInChildren<Text>(true);
            dropdown.itemText = itemText != null ? itemText : CreateDropdownItemText(itemToggle.transform);
        }

        if (itemToggle.targetGraphic == null)
        {
            Image targetImage = itemToggle.GetComponent<Image>();
            if (targetImage == null)
                targetImage = itemToggle.gameObject.AddComponent<Image>();

            itemToggle.targetGraphic = targetImage;
        }

        if (scrollRect != null)
        {
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
        }

        template.gameObject.SetActive(wasActive);
    }

    private RectTransform CreateDropdownTemplate(Dropdown dropdown)
    {
        GameObject templateObject = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform template = templateObject.GetComponent<RectTransform>();
        template.SetParent(dropdown.transform, false);
        template.anchorMin = new Vector2(0f, 0f);
        template.anchorMax = new Vector2(1f, 0f);
        template.pivot = new Vector2(0.5f, 1f);
        template.anchoredPosition = Vector2.zero;
        template.sizeDelta = new Vector2(0f, AppConfig.MainMenu.DropdownTemplateHeight);
        templateObject.SetActive(false);

        Image image = templateObject.GetComponent<Image>();
        image.color = AppConfig.MainMenu.Transparent;

        return template;
    }

    private RectTransform FindOrCreateRectTransform(RectTransform root, string path, string fallbackName)
    {
        Transform found = root.Find(path);
        if (found == null)
            found = root.Find(fallbackName);

        if (found != null && found.TryGetComponent(out RectTransform existingRect))
            return existingRect;

        GameObject created = new GameObject(fallbackName, typeof(RectTransform));
        RectTransform rect = created.GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, AppConfig.MainMenu.DropdownLabelHeight);
        return rect;
    }

    private Toggle CreateDropdownTemplateItem(RectTransform content)
    {
        GameObject itemObject = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle));
        RectTransform itemRect = itemObject.GetComponent<RectTransform>();
        itemRect.SetParent(content, false);
        itemRect.anchorMin = new Vector2(0f, 0.5f);
        itemRect.anchorMax = new Vector2(1f, 0.5f);
        itemRect.pivot = new Vector2(0.5f, 0.5f);
        itemRect.sizeDelta = new Vector2(0f, AppConfig.MainMenu.DropdownItemHeight);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.color = AppConfig.MainMenu.Transparent;

        Toggle toggle = itemObject.GetComponent<Toggle>();
        toggle.targetGraphic = itemImage;
        toggle.isOn = true;
        CreateDropdownItemText(itemObject.transform);
        return toggle;
    }

    private Text CreateDropdownItemText(Transform parent)
    {
        GameObject textObject = new GameObject("Item Label", typeof(RectTransform), typeof(Text));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(AppConfig.MainMenu.DropdownHorizontalTextPadding, 1f);
        textRect.offsetMax = new Vector2(-AppConfig.MainMenu.DropdownHorizontalTextPadding, -1f);

        Text text = textObject.GetComponent<Text>();
        text.text = "Option";
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return text;
    }

    private void ConfigureLanguageDropdown(TMP_Dropdown dropdown, Sprite[] flags)
    {
        languageTmpDropdown = dropdown;
        AddLocalizedIgnore(dropdown.gameObject);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.captionImage = ConfigureFlagImage(dropdown.captionText, dropdown.captionImage);
        dropdown.itemImage = ConfigureFlagImage(dropdown.itemText, dropdown.itemImage);
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>(languageOptions.Length);
        for (int i = 0; i < languageOptions.Length; i++)
            options.Add(new TMP_Dropdown.OptionData(flags[i] == null ? languageOptions[i].Label : "", flags[i], Color.white));
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(GetCurrentLanguageIndex());
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    private void OnLanguageDropdownChanged(int value)
    {
        AppLanguage language = AppLanguage.Ukrainian;
        if (value >= 0 && value < languageOptions.Length)
            language = languageOptions[value].Language;

        localization.SetLanguage(language);
        owner.RefreshCharacterList();
        localization.ApplyToScene();
        SyncLanguageDropdownValue();
    }

    private int GetCurrentLanguageIndex()
    {
        AppLanguage current = localization.CurrentLanguage;
        for (int i = 0; i < languageOptions.Length; i++)
        {
            if (languageOptions[i].Language == current)
                return i;
        }

        return 0;
    }

    private IEnumerator SyncLanguageDropdownNextFrame()
    {
        yield return null;
        SyncLanguageDropdownValue();
        yield return null;
        SyncLanguageDropdownValue();
    }

    internal void SyncLanguageDropdownValue()
    {
        int index = GetCurrentLanguageIndex();

        if (languageDropdown == null)
            languageDropdown = sceneLookup.FindFirstDropdownInScene("localiza", "LanguageDropdown");

        if (languageDropdown != null)
        {
            languageDropdown.SetValueWithoutNotify(index);
            languageDropdown.RefreshShownValue();
        }

        if (languageTmpDropdown == null)
            languageTmpDropdown = sceneLookup.FindFirstTmpDropdownInScene("localiza", "LanguageDropdown");

        if (languageTmpDropdown != null)
        {
            languageTmpDropdown.SetValueWithoutNotify(index);
            languageTmpDropdown.RefreshShownValue();
        }
    }

    private void AddLocalizedIgnore(GameObject target)
    {
        if (target != null && target.GetComponent<LocalizedIgnore>() == null)
            target.AddComponent<LocalizedIgnore>();
    }

    private void RemoveOldLanguageButtons()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform item in transforms)
        {
            if (!sceneLookup.IsSceneObject(item.gameObject))
                continue;

            if (item.name == "LanguageButtons")
                Destroy(item.gameObject);
        }
    }

}
