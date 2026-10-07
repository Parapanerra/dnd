using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using SimpleFileBrowser;
using TMPro;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("Required")]
    public Transform characterListContent;
    public GameObject characterButtonPrefab;
    public Button createNewCharacterButton;

    [Header("Scenes")]
    public string characterSheetSceneName = AppConfig.Scenes.CharacterSheet;
    public string inventorySceneName = AppConfig.Scenes.Inventory;
    public string spellbookSceneName = AppConfig.Scenes.Spellbook;

    [Header("Layout")]
    public float characterRowSpacing = AppConfig.MainMenu.CharacterRowSpacing;

    [HideInInspector] public Transform characterRowsContent;
    [HideInInspector] public GameObject characterRowTemplate;
    [HideInInspector] public GameObject characterButtonTemplate;
    [HideInInspector] public Button addCharacterButton;
    [HideInInspector] public bool openCharacterAfterCreate;
    [HideInInspector] public bool useCharacterButtonPrefab = true;
    [HideInInspector] public bool repairScrollViewAtRuntime;
    [HideInInspector] public bool applyDefaultCharacterListLayout;
    [HideInInspector] public bool applyDefaultCharacterButtonStyle;
    [HideInInspector] public float characterButtonSpacing = AppConfig.MainMenu.CharacterButtonSpacing;

    [HideInInspector] public Button importButton;
    [HideInInspector] public Button exportButton;

    private Button exportOneCharacterButton;
    private Button importOneCharacterButton;
    private Button openSavePanelButton;
    private Dropdown languageDropdown;
    private TMP_Dropdown languageTmpDropdown;
    private Dropdown oneCharacterDropdown;
    private TMP_Dropdown oneCharacterTmpDropdown;
    private GameObject savePanel;
    private bool oneCharacterDropdownHasSelection;
    private MainMenuCharacterListView characterListView;
    private float lastCharacterCreateTime = -AppConfig.MainMenu.CharacterCreateDebounceSeconds;

    // RU is temporarily hidden. Uncomment the Russian entries below to re-enable.
    private static readonly AppLanguage[] SupportedDropdownLanguages = new AppLanguage[]
    {
        AppLanguage.Ukrainian,
        AppLanguage.English
        // AppLanguage.Russian
    };

    private static readonly List<string> SupportedDropdownLanguageLabels = new List<string>
    {
        "UA",
        "EN"
        // "RU"
    };

    private void OnEnable()
    {
        StartCoroutine(SyncLanguageDropdownNextFrame());
    }

    private void Start()
    {
        NormalizeSceneNames();
        DndSaveManager.EnsureExists();
        RuntimeLocalization.EnsureExists();
        characterListView = GetComponent<MainMenuCharacterListView>();
        if (characterListView == null)
            characterListView = gameObject.AddComponent<MainMenuCharacterListView>();
        characterListView.Initialize(this);
        characterListView.Wire();
        WireSaveFileButtons();
        if (savePanel != null)
            savePanel.SetActive(false);

        if (repairScrollViewAtRuntime)
            characterListView.EnsureScrollViewIsVisible();
        if (applyDefaultCharacterListLayout)
            characterListView.EnsureCharacterListLayout();
        EnsureLanguageDropdown();
        characterListView.CacheCharacterButtonTemplate();
        characterListView.DisableAutomaticContentLayout();

        RefreshCharacterList();
        RuntimeLocalization.EnsureExists().ApplyToScene();
        SyncLanguageDropdownValue();

        if (addCharacterButton != null)
        {
            addCharacterButton.onClick.RemoveAllListeners();
            addCharacterButton.onClick.AddListener(OnCreateNewCharacterClicked);
        }

        if (createNewCharacterButton != null)
        {
            createNewCharacterButton.onClick.RemoveAllListeners();
            createNewCharacterButton.onClick.AddListener(OnCreateNewCharacterClicked);
        }

        if (exportButton != null)
        {
            exportButton.onClick.RemoveAllListeners();
            exportButton.onClick.AddListener(ExportFile);
        }

        if (importButton != null)
        {
            importButton.onClick.RemoveAllListeners();
            importButton.onClick.AddListener(ImportFile);
        }

        if (exportOneCharacterButton != null)
        {
            exportOneCharacterButton.onClick.RemoveAllListeners();
            exportOneCharacterButton.onClick.AddListener(ExportSelectedCharacterFile);
        }

        if (importOneCharacterButton != null)
        {
            importOneCharacterButton.onClick.RemoveAllListeners();
            importOneCharacterButton.onClick.AddListener(ImportCharacterFile);
        }

        if (oneCharacterDropdown != null)
        {
            oneCharacterDropdown.onValueChanged.RemoveAllListeners();
            oneCharacterDropdown.onValueChanged.AddListener(OnOneCharacterDropdownChanged);
            AddOneCharacterDropdownClickListener(oneCharacterDropdown.gameObject);
        }

        if (oneCharacterTmpDropdown != null)
        {
            oneCharacterTmpDropdown.onValueChanged.RemoveAllListeners();
            oneCharacterTmpDropdown.onValueChanged.AddListener(OnOneCharacterDropdownChanged);
            AddOneCharacterDropdownClickListener(oneCharacterTmpDropdown.gameObject);
        }

        if (openSavePanelButton != null && savePanel != null)
        {
            openSavePanelButton.onClick.RemoveAllListeners();
            openSavePanelButton.onClick.AddListener(ToggleSavePanel);
        }

        string storageError = DndSaveManager.Instance != null ? DndSaveManager.Instance.SaveError : null;
        if (!string.IsNullOrEmpty(storageError))
            TaruckImportReviewDialog.Show("Стан сховища", storageError, "Закрити", () => { });
    }

    private void NormalizeSceneNames()
    {
        if (string.IsNullOrWhiteSpace(characterSheetSceneName) || characterSheetSceneName == "CharacterSheetScene")
            characterSheetSceneName = AppConfig.Scenes.CharacterSheet;

        if (string.IsNullOrWhiteSpace(inventorySceneName))
            inventorySceneName = AppConfig.Scenes.Inventory;

        if (string.IsNullOrWhiteSpace(spellbookSceneName))
            spellbookSceneName = AppConfig.Scenes.Spellbook;
    }

    public void EnsureEditableCharacterScrollView()
    {
        if (this == null)
            return;

        Transform parent = transform.parent != null ? transform.parent : transform;
        Transform existingScrollView = parent.Find("CharacterRowsScrollView");
        if (existingScrollView != null)
        {
            WireEditableCharacterScrollView(existingScrollView);
            return;
        }

        GameObject scrollView = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "CharacterRowsScrollView",
            parent,
            AppConfig.MainMenu.CenterAnchor,
            AppConfig.MainMenu.CharacterScrollPosition,
            AppConfig.MainMenu.CharacterScrollSize));
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = AppConfig.MainMenu.Transparent;
        scrollImage.raycastTarget = false;
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "Viewport",
            scrollView.transform,
            AppConfig.MainMenu.CenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterScrollSize));
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = AppConfig.MainMenu.Transparent;
        viewportImage.raycastTarget = false;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "RowsContent",
            viewport.transform,
            AppConfig.MainMenu.TopCenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterScrollSize));
        GameObject rowTemplate = CreateDefaultCharacterRowTemplate(content.transform);
        GameObject addButtonObject = RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "AddCharacterButton",
            scrollView.transform,
            AppConfig.MainMenu.AddButtonPosition,
            AppConfig.MainMenu.AddButtonSize,
            "Додати персонажа"));

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = content.GetComponent<RectTransform>();

        characterRowsContent = content.transform;
        characterRowTemplate = rowTemplate;
        addCharacterButton = addButtonObject.GetComponent<Button>();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.EditorUtility.SetDirty(scrollView);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    private void WireEditableCharacterScrollView(Transform scrollView)
    {
        Transform viewport = scrollView.Find("Viewport");
        Transform content = viewport != null ? viewport.Find("RowsContent") : scrollView.Find("RowsContent");
        Transform rowTemplate = content != null ? content.Find("CharacterRowTemplate") : null;
        Transform addButton = scrollView.Find("AddCharacterButton");

        if (content != null)
            characterRowsContent = content;
        if (rowTemplate != null)
            characterRowTemplate = rowTemplate.gameObject;
        if (addButton != null)
            addCharacterButton = addButton.GetComponent<Button>();
    }

    private GameObject CreateDefaultCharacterRowTemplate(Transform parent)
    {
        GameObject row = RuntimeUiFactory.CreateElement(RuntimeUiElementSpec.Create(
            "CharacterRowTemplate",
            parent,
            AppConfig.MainMenu.TopCenterAnchor,
            Vector2.zero,
            AppConfig.MainMenu.CharacterRowSize));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "InventoryButton", row.transform, AppConfig.MainMenu.InventoryButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "I"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "CharacterButton", row.transform, AppConfig.MainMenu.CharacterButtonPosition, AppConfig.MainMenu.CharacterNameButtonSize, "Персонаж №1"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "SpellsButton", row.transform, AppConfig.MainMenu.SpellsButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "S"));
        RuntimeUiFactory.CreateButton(RuntimeButtonSpec.Create(
            "DeleteButton", row.transform, AppConfig.MainMenu.DeleteRowButtonPosition, AppConfig.MainMenu.RowActionButtonSize, "X"));
        return row;
    }

    private void EnsureLanguageDropdown()
    {
        RemoveOldLanguageButtons();

        if (RuntimeLocalization.EnsureExists().CurrentLanguage == AppLanguage.Russian)
        {
            RuntimeLocalization.EnsureExists().SetLanguage(AppLanguage.Ukrainian);
        }

        Dropdown dropdown = FindFirstDropdownInScene("localiza", "LanguageDropdown");
        TMP_Dropdown tmpDropdown = FindFirstTmpDropdownInScene("localiza", "LanguageDropdown");

        if (dropdown == null && tmpDropdown == null)
        {
            Debug.LogWarning("MainMenuManager: cannot find language dropdown 'localiza'.");
            return;
        }

        if (dropdown != null)
            ConfigureLanguageDropdown(dropdown);

        if (tmpDropdown != null)
            ConfigureLanguageDropdown(tmpDropdown);
    }

    private void ConfigureLanguageDropdown(Dropdown dropdown)
    {
        languageDropdown = dropdown;
        AddLocalizedIgnore(dropdown.gameObject);
        RepairDropdownTemplate(dropdown);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.AddOptions(SupportedDropdownLanguageLabels);
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

        Text itemText = itemToggle.GetComponentInChildren<Text>(true);
        if (itemText == null)
            itemText = CreateDropdownItemText(itemToggle.transform);

        if (itemToggle.targetGraphic == null)
        {
            Image targetImage = itemToggle.GetComponent<Image>();
            if (targetImage == null)
                targetImage = itemToggle.gameObject.AddComponent<Image>();

            itemToggle.targetGraphic = targetImage;
        }

        dropdown.itemText = itemText;
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

    private void ConfigureLanguageDropdown(TMP_Dropdown dropdown)
    {
        languageTmpDropdown = dropdown;
        AddLocalizedIgnore(dropdown.gameObject);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.AddOptions(SupportedDropdownLanguageLabels);
        dropdown.SetValueWithoutNotify(GetCurrentLanguageIndex());
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    private void OnLanguageDropdownChanged(int value)
    {
        AppLanguage language = AppLanguage.Ukrainian;
        if (value >= 0 && value < SupportedDropdownLanguages.Length)
            language = SupportedDropdownLanguages[value];

        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        localization.SetLanguage(language);
        RefreshCharacterList();
        localization.ApplyToScene();
        SyncLanguageDropdownValue();
    }

    private int GetCurrentLanguageIndex()
    {
        AppLanguage current = RuntimeLocalization.EnsureExists().CurrentLanguage;
        for (int i = 0; i < SupportedDropdownLanguages.Length; i++)
        {
            if (SupportedDropdownLanguages[i] == current)
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

    private void SyncLanguageDropdownValue()
    {
        int index = GetCurrentLanguageIndex();

        if (languageDropdown == null)
            languageDropdown = FindFirstDropdownInScene("localiza", "LanguageDropdown");

        if (languageDropdown != null)
        {
            languageDropdown.SetValueWithoutNotify(index);
            languageDropdown.RefreshShownValue();
        }

        if (languageTmpDropdown == null)
            languageTmpDropdown = FindFirstTmpDropdownInScene("localiza", "LanguageDropdown");

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
            if (!IsSceneObject(item.gameObject))
                continue;

            if (item.name == "LanguageButtons")
                Destroy(item.gameObject);
        }
    }

    private void WireSaveFileButtons()
    {
        Button downloadButton = FindFirstButtonInScene("dowload", "download");
        if (downloadButton != null)
            exportButton = downloadButton;

        Button uploadButton = FindButtonInScene("upload");
        if (uploadButton != null)
            importButton = uploadButton;

        exportOneCharacterButton = FindButtonInScene("downLoadOne");
        importOneCharacterButton = FindButtonInScene("upLoadOne");
        oneCharacterDropdown = FindDropdownInScene("choisSeveFailPersonsj");
        oneCharacterTmpDropdown = FindTmpDropdownInScene("choisSeveFailPersonsj");
        openSavePanelButton = FindButtonInScene("openPanelSaveBatton");

        Transform panelTransform = FindTransformInScene("openPanelSavePanel");
        savePanel = panelTransform != null ? panelTransform.gameObject : null;

        RefreshOneCharacterDropdown();
    }

    private void ExportFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        saveManager.SaveData();

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".tall"));
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    try
                    {
                        TaruckCharacterTransfer.WriteCollection(paths[0], saveManager.saveData, Application.version);
                        TaruckImportReviewDialog.Show("Експорт", "Колекцію персонажів збережено.", "Закрити", () => { });
                    }
                    catch (System.Exception exception)
                    {
                        TaruckImportReviewDialog.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                    }
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckCharacterTransfer.DefaultFileBrowserPath(),
            "AllCharacters.tall",
            TaruckImportReviewDialog.Text("Зберегти файл", "Save file"),
            TaruckImportReviewDialog.Text("Зберегти", "Save")
        );
    }

    private void ImportFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            TaruckImportReviewDialog.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tall", ".taruck-save", ".json"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

                byte[] bytes = null;
                try
                {
                    AppSaveData importedData = TaruckCharacterTransfer.ReadCollection(paths[0], out bool binary, out bytes);
                    string preview = (binary ? "Taruck v1" : TaruckImportReviewDialog.Text("Старий JSON", "Legacy JSON")) +
                        TaruckImportReviewDialog.Text("\nПерсонажів: ", "\nCharacters: ") + importedData.characters.Count;
                    for (int i = 0; i < Mathf.Min(importedData.characters.Count, 50); i++)
                        preview += "\n• " + importedData.characters[i].characterName;
                    if (importedData.characters.Count > 50)
                        preview += TaruckImportReviewDialog.Text("\n… ще ", "\n… and ") + (importedData.characters.Count - 50);
                    TaruckImportReviewDialog.Show("Перевірка імпорту", preview +
                        TaruckImportReviewDialog.Text("\n\nЗамінити всю колекцію чи додати персонажів?",
                            "\n\nReplace the whole collection or add these characters?"),
                        "Замінити все", () => ApplyCollectionImport(saveManager, importedData, false),
                        "Додати", () => ApplyCollectionImport(saveManager, importedData, true));
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка імпорту",
                        TaruckImportReviewDialog.ImportError(exception, bytes, TaruckFileType.FullSaveExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckCharacterTransfer.DefaultFileBrowserPath(),
            null,
            TaruckImportReviewDialog.Text("Виберіть файл Taruck або JSON", "Select Taruck or JSON file"),
            TaruckImportReviewDialog.Text("Вибрати", "Select")
        );
    }

    private void ExportSelectedCharacterFile()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string fileName = TaruckCharacterTransfer.SafeFileName(character.characterName, "DnDCharacter") + ".tchar";

        FileBrowser.SetFilters(false, new FileBrowser.Filter("Taruck", ".tchar"));
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                try
                {
                    TaruckCharacterTransfer.WriteCharacter(paths[0], character, Application.version);
                    TaruckImportReviewDialog.Show("Експорт", "Персонажа збережено.", "Закрити", () => { });
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка експорту", exception.Message, "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckCharacterTransfer.DefaultFileBrowserPath(),
            fileName,
            TaruckImportReviewDialog.Text("Зберегти персонажа", "Save character"),
            TaruckImportReviewDialog.Text("Зберегти", "Save")
        );
    }

    private void ImportCharacterFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter(
            TaruckImportReviewDialog.Text("Taruck або старий JSON", "Taruck or legacy JSON"),
            ".tchar", ".taruck-character", ".json", ".dndchar"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                byte[] bytes = null;
                try
                {
                    CharacterData importedCharacter = TaruckCharacterTransfer.ReadCharacter(paths[0], out bytes);
                    TaruckImportReviewDialog.Show("Перевірка персонажа",
                        TaruckImportReviewDialog.Text("Імпортувати персонажа «", "Import character “") +
                            importedCharacter.characterName + TaruckImportReviewDialog.Text("»?", "”?"),
                        "Додати", () => ApplyCharacterImport(saveManager, importedCharacter));
                }
                catch (System.Exception exception)
                {
                    TaruckImportReviewDialog.Show("Помилка імпорту",
                        TaruckImportReviewDialog.ImportError(exception, bytes, TaruckFileType.CharacterExport),
                        "Закрити", () => { });
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            TaruckCharacterTransfer.DefaultFileBrowserPath(),
            null,
            TaruckImportReviewDialog.Text("Виберіть файл персонажа", "Select character file"),
            TaruckImportReviewDialog.Text("Вибрати", "Select")
        );
    }

    private void ApplyCollectionImport(DndSaveManager manager, AppSaveData imported, bool merge)
    {
        if (!manager.TryImportCollection(imported, merge, out string error))
        {
            TaruckImportReviewDialog.Show("Помилка імпорту", error, "Закрити", () => { });
            return;
        }
        RefreshCharacterList();
        TaruckImportReviewDialog.Show("Імпорт завершено",
            TaruckImportReviewDialog.Text("Персонажів у колекції: ", "Characters in collection: ") + manager.saveData.characters.Count,
            "Закрити", () => { });
    }

    private void ApplyCharacterImport(DndSaveManager manager, CharacterData imported)
    {
        try
        {
            string importedId = TaruckCharacterTransfer.ImportCharacter(manager, imported, Application.version);
            RefreshCharacterList();
            OpenImportedCharacter(importedId);
        }
        catch (System.Exception exception)
        {
            TaruckImportReviewDialog.Show("Помилка імпорту", exception.Message, "Закрити", () => { });
        }
    }

    private CharacterData GetSelectedCharacterForExport()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        if (saveManager.saveData == null || saveManager.saveData.characters == null || saveManager.saveData.characters.Count == 0)
            return null;

        if (!oneCharacterDropdownHasSelection)
            return null;

        int index = oneCharacterDropdown != null
            ? oneCharacterDropdown.value
            : oneCharacterTmpDropdown != null ? oneCharacterTmpDropdown.value : 0;

        index = Mathf.Clamp(index, 0, saveManager.saveData.characters.Count - 1);
        return saveManager.saveData.characters[index];
    }

    private void RefreshOneCharacterDropdown()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        List<string> options = new List<string>();
        bool hasCharacters = saveManager.saveData != null &&
                             saveManager.saveData.characters != null &&
                             saveManager.saveData.characters.Count > 0;
        oneCharacterDropdownHasSelection = false;

        if (hasCharacters)
        {
            foreach (CharacterData character in saveManager.saveData.characters)
                options.Add(GetMenuCharacterName(character));
        }
        else
        {
            options.Add(LocalizeMenuText("Немає персонажів"));
        }

        if (oneCharacterDropdown != null)
        {
            oneCharacterDropdown.ClearOptions();
            oneCharacterDropdown.AddOptions(options);
            oneCharacterDropdown.SetValueWithoutNotify(0);
            oneCharacterDropdown.RefreshShownValue();
            oneCharacterDropdown.interactable = hasCharacters;
            if (hasCharacters)
                ApplyOneCharacterDropdownPlaceholder();
        }

        if (oneCharacterTmpDropdown != null)
        {
            oneCharacterTmpDropdown.ClearOptions();
            oneCharacterTmpDropdown.AddOptions(options);
            oneCharacterTmpDropdown.SetValueWithoutNotify(0);
            oneCharacterTmpDropdown.RefreshShownValue();
            oneCharacterTmpDropdown.interactable = hasCharacters;
            if (hasCharacters)
                ApplyOneCharacterDropdownPlaceholder();
        }

        UpdateOneCharacterExportButtonState();
    }

    private void UpdateOneCharacterExportButtonState()
    {
        if (exportOneCharacterButton == null)
            return;

        bool hasSelection = oneCharacterDropdownHasSelection && (oneCharacterDropdown != null
            ? oneCharacterDropdown.interactable
            : oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.interactable);

        exportOneCharacterButton.interactable = hasSelection;
    }

    private void ToggleSavePanel()
    {
        if (savePanel == null)
            return;

        bool shouldShow = !savePanel.activeSelf;
        savePanel.SetActive(shouldShow);

        if (shouldShow)
            StartCoroutine(RestoreOneCharacterDropdownCaptionNextFrame());
    }

    private IEnumerator RestoreOneCharacterDropdownCaptionNextFrame()
    {
        RestoreOneCharacterDropdownCaption();
        yield return null;
        RestoreOneCharacterDropdownCaption();
    }

    private void RestoreOneCharacterDropdownCaption()
    {
        if (!HasOneCharacterDropdownOptions())
            return;

        if (oneCharacterDropdownHasSelection)
            ApplySelectedOneCharacterDropdownCaption();
        else
            ApplyOneCharacterDropdownPlaceholder();
    }

    private void OnOneCharacterDropdownChanged(int value)
    {
        oneCharacterDropdownHasSelection = HasOneCharacterDropdownOptions();
        ApplySelectedOneCharacterDropdownCaption();
        UpdateOneCharacterExportButtonState();
    }

    private void OnOneCharacterDropdownClicked()
    {
        if (!HasOneCharacterDropdownOptions())
            return;

        oneCharacterDropdownHasSelection = true;
        ApplySelectedOneCharacterDropdownCaption();
        UpdateOneCharacterExportButtonState();
    }

    private bool HasOneCharacterDropdownOptions()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();
        return saveManager.saveData != null &&
               saveManager.saveData.characters != null &&
               saveManager.saveData.characters.Count > 0;
    }

    private void ApplyOneCharacterDropdownPlaceholder()
    {
        if (oneCharacterDropdown != null && oneCharacterDropdown.captionText != null)
            oneCharacterDropdown.captionText.text = LocalizeMenuText("Оберіть персонажа");

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = LocalizeMenuText("Оберіть персонажа");
    }

    private void ApplySelectedOneCharacterDropdownCaption()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string label = GetMenuCharacterName(character);

        if (oneCharacterDropdown != null && oneCharacterDropdown.captionText != null)
            oneCharacterDropdown.captionText.text = label;

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = label;
    }

    private void AddOneCharacterDropdownClickListener(GameObject dropdownObject)
    {
        if (dropdownObject == null)
            return;

        EventTrigger trigger = dropdownObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = dropdownObject.AddComponent<EventTrigger>();

        EventTrigger.Entry clickEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        clickEntry.callback.AddListener(delegate { OnOneCharacterDropdownClicked(); });
        trigger.triggers.Add(clickEntry);
    }

    internal string GetMenuCharacterName(CharacterData character, string emptyFallback = "Новий персонаж")
    {
        string rawName = character != null ? character.characterName : "";
        if (TryGetDefaultCharacterNumber(rawName, out int number))
        {
            string localizedBase = LocalizeMenuText("Новий персонаж");
            return number > 0 ? localizedBase + " " + number : localizedBase;
        }

        if (string.IsNullOrWhiteSpace(rawName))
            return LocalizeMenuText(emptyFallback);

        return rawName;
    }

    private bool TryGetDefaultCharacterNumber(string name, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(name))
            return true;

        string normalized = name.Trim();
        string[] bases = { "Новий персонаж", "New character", "Новый персонаж" };
        foreach (string baseName in bases)
        {
            if (string.Equals(normalized, baseName, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (!normalized.StartsWith(baseName + " ", System.StringComparison.OrdinalIgnoreCase))
                continue;

            string suffix = normalized.Substring(baseName.Length).Trim();
            if (int.TryParse(suffix, out number))
                return true;
        }

        return false;
    }

    private string LocalizeMenuText(string source)
    {
        return RuntimeLocalization.EnsureExists().Translate(source);
    }

    public void RefreshCharacterList()
    {
        RefreshOneCharacterDropdown();
        if (characterListView != null)
            characterListView.Refresh();
    }

    private void OnCreateNewCharacterClicked()
    {
        if (Time.unscaledTime - lastCharacterCreateTime <
            AppConfig.MainMenu.CharacterCreateDebounceSeconds)
            return;

        lastCharacterCreateTime = Time.unscaledTime;

        CharacterData newChar = DndSaveManager.Instance.CreateNewCharacter();
        RefreshCharacterList();

        if (openCharacterAfterCreate)
            OnCharacterSelected(newChar.id);
    }

    private void OpenImportedCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return;

        OnCharacterSelected(characterId);
    }

    internal void OnCharacterSelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;
        
        SceneManager.LoadScene(characterSheetSceneName);
    }

    internal void OnInventorySelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(inventorySceneName);
    }

    internal void OnSpellbookSelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(spellbookSceneName);
    }

    internal Button FindButtonInScene(string objectName)
    {
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (Button button in buttons)
        {
            if (!IsSceneObject(button.gameObject))
                continue;

            if (NamesMatch(button.gameObject.name, objectName))
                return button;
        }

        return null;
    }

    private Button FindFirstButtonInScene(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            Button button = FindButtonInScene(objectName);
            if (button != null)
                return button;
        }

        return null;
    }

    private Transform FindTransformInScene(string objectName)
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in transforms)
        {
            if (!IsSceneObject(transform.gameObject))
                continue;

            if (NamesMatch(transform.gameObject.name, objectName))
                return transform;
        }

        return null;
    }

    private Dropdown FindDropdownInScene(string objectName)
    {
        Dropdown[] dropdowns = Resources.FindObjectsOfTypeAll<Dropdown>();
        foreach (Dropdown dropdown in dropdowns)
        {
            if (!IsSceneObject(dropdown.gameObject))
                continue;

            if (NamesMatch(dropdown.gameObject.name, objectName))
                return dropdown;
        }

        return null;
    }

    private Dropdown FindFirstDropdownInScene(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            Dropdown dropdown = FindDropdownInScene(objectName);
            if (dropdown != null)
                return dropdown;
        }

        return null;
    }

    private TMP_Dropdown FindTmpDropdownInScene(string objectName)
    {
        TMP_Dropdown[] dropdowns = Resources.FindObjectsOfTypeAll<TMP_Dropdown>();
        foreach (TMP_Dropdown dropdown in dropdowns)
        {
            if (!IsSceneObject(dropdown.gameObject))
                continue;

            if (NamesMatch(dropdown.gameObject.name, objectName))
                return dropdown;
        }

        return null;
    }

    private TMP_Dropdown FindFirstTmpDropdownInScene(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            TMP_Dropdown dropdown = FindTmpDropdownInScene(objectName);
            if (dropdown != null)
                return dropdown;
        }

        return null;
    }

    internal ScrollRect FindScrollRectInScene(string objectName)
    {
        ScrollRect[] scrollRects = Resources.FindObjectsOfTypeAll<ScrollRect>();
        foreach (ScrollRect scrollRect in scrollRects)
        {
            if (!IsSceneObject(scrollRect.gameObject))
                continue;

            if (NamesMatch(scrollRect.gameObject.name, objectName))
                return scrollRect;
        }

        return null;
    }

    internal Button FindButtonUnder(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (NamesMatch(button.gameObject.name, objectName))
                return button;
        }

        return null;
    }

    internal Button FindFirstDirectButtonInContent(Transform content)
    {
        if (content == null)
            return null;

        foreach (Transform child in content)
        {
            Button button = child.GetComponent<Button>();
            if (button != null)
                return button;

            button = child.GetComponentInChildren<Button>(true);
            if (button != null)
                return button;
        }

        return null;
    }

    internal Button FindFirstButtonUnder(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Button button = FindButtonUnder(root, name);
            if (button != null)
                return button;
        }

        return null;
    }

    internal bool NamesMatch(string actualName, string expectedName)
    {
        return string.Equals(actualName.Trim(), expectedName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSceneObject(GameObject gameObject)
    {
        return gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.name);
    }

    internal void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

}
