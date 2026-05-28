using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using SimpleFileBrowser;
using System.IO;
using TMPro;
using UnityEngine.EventSystems;

public class MainMenuManager : MonoBehaviour
{
    [Header("Required")]
    public Transform characterListContent;
    public GameObject characterButtonPrefab;
    public Button createNewCharacterButton;

    [Header("Scenes")]
    public string characterSheetSceneName = "cartaPersonaj";
    public string inventorySceneName = "inventory";
    public string spellbookSceneName = "spelBook";

    [Header("Layout")]
    public float characterRowSpacing = 12f;

    [HideInInspector] public Transform characterRowsContent;
    [HideInInspector] public GameObject characterRowTemplate;
    [HideInInspector] public GameObject characterButtonTemplate;
    [HideInInspector] public Button addCharacterButton;
    [HideInInspector] public bool openCharacterAfterCreate;
    [HideInInspector] public bool useCharacterButtonPrefab = true;
    [HideInInspector] public bool repairScrollViewAtRuntime;
    [HideInInspector] public bool applyDefaultCharacterListLayout;
    [HideInInspector] public bool applyDefaultCharacterButtonStyle;
    [HideInInspector] public float characterButtonSpacing = 12f;

    [HideInInspector] public Button importButton;
    [HideInInspector] public Button exportButton;

    private const float CharacterButtonHeight = 95f;
    private Button exportOneCharacterButton;
    private Button importOneCharacterButton;
    private Button openSavePanelButton;
    private Dropdown oneCharacterDropdown;
    private TMP_Dropdown oneCharacterTmpDropdown;
    private GameObject savePanel;
    private bool oneCharacterDropdownHasSelection;
    private Button userCharacterButtonTemplate;
    private Transform userMenuRoot;
    private Transform userMenuContent;
    private RectTransform lastUserMenuRowRect;
    private Vector3 addButtonWorldOffsetFromTemplate;
    private bool hasAddButtonWorldOffsetFromTemplate;
    private Coroutine addButtonPositionCoroutine;
    private float lastCharacterCreateTime = -10f;

    private void Start()
    {
        NormalizeSceneNames();
        DndSaveManager.EnsureExists();
        RuntimeLocalization.EnsureExists();
        WireUserCreatedMenu();
        WireSaveFileButtons();
        if (savePanel != null)
            savePanel.SetActive(false);

        if (repairScrollViewAtRuntime)
            EnsureScrollViewIsVisible();
        if (applyDefaultCharacterListLayout)
            EnsureCharacterListLayout();
        EnsureLanguageDropdown();
        CacheCharacterButtonTemplate();
        DisableAutomaticContentLayout();

        RefreshCharacterList();
        RuntimeLocalization.EnsureExists().ApplyToScene();

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
    }

    private void NormalizeSceneNames()
    {
        if (string.IsNullOrWhiteSpace(characterSheetSceneName) || characterSheetSceneName == "CharacterSheetScene")
            characterSheetSceneName = "cartaPersonaj";

        if (string.IsNullOrWhiteSpace(inventorySceneName))
            inventorySceneName = "inventory";

        if (string.IsNullOrWhiteSpace(spellbookSceneName))
            spellbookSceneName = "spelBook";
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

        GameObject scrollView = CreateRuntimeUiObject("CharacterRowsScrollView", parent, new Vector2(0.5f, 0.5f), new Vector2(0f, -165f), new Vector2(560f, 520f));
        Image scrollImage = scrollView.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0f);
        scrollImage.raycastTarget = false;
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateRuntimeUiObject("Viewport", scrollView.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 520f));
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        viewportImage.raycastTarget = false;
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateRuntimeUiObject("RowsContent", viewport.transform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(560f, 520f));
        GameObject rowTemplate = CreateDefaultCharacterRowTemplate(content.transform);
        GameObject addButtonObject = CreateDefaultButton("AddCharacterButton", scrollView.transform, new Vector2(0f, -292f), new Vector2(310f, 58f), "Додати персонажа");

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
        GameObject row = CreateRuntimeUiObject("CharacterRowTemplate", parent, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(560f, 78f));
        CreateDefaultButton("InventoryButton", row.transform, new Vector2(-235f, 0f), new Vector2(66f, 66f), "I");
        CreateDefaultButton("CharacterButton", row.transform, new Vector2(-25f, 0f), new Vector2(330f, 66f), "Персонаж №1");
        CreateDefaultButton("SpellsButton", row.transform, new Vector2(185f, 0f), new Vector2(66f, 66f), "S");
        CreateDefaultButton("DeleteButton", row.transform, new Vector2(265f, 0f), new Vector2(66f, 66f), "X");
        return row;
    }

    private GameObject CreateDefaultButton(string name, Transform parent, Vector2 position, Vector2 size, string label)
    {
        GameObject buttonObject = CreateRuntimeUiObject(name, parent, new Vector2(0.5f, 0.5f), position, size);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.09f, 0.07f, 0.9f);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject textObject = CreateRuntimeUiObject("Text", buttonObject.transform, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = label;
        text.color = new Color(0.95f, 0.55f, 0.05f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = 28;
        text.raycastTarget = false;

        return buttonObject;
    }

    private GameObject CreateRuntimeUiObject(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return gameObject;
    }

    private void EnsureLanguageDropdown()
    {
        RemoveOldLanguageButtons();

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
        AddLocalizedIgnore(dropdown.gameObject);
        RepairDropdownTemplate(dropdown);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "UA", "EN", "RU" });
        dropdown.value = GetCurrentLanguageIndex();
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
        template.sizeDelta = new Vector2(0f, 90f);
        templateObject.SetActive(false);

        Image image = templateObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);

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
        rect.sizeDelta = new Vector2(0f, 28f);
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
        itemRect.sizeDelta = new Vector2(0f, 24f);

        Image itemImage = itemObject.GetComponent<Image>();
        itemImage.color = new Color(1f, 1f, 1f, 0f);

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
        textRect.offsetMin = new Vector2(8f, 1f);
        textRect.offsetMax = new Vector2(-8f, -1f);

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
        AddLocalizedIgnore(dropdown.gameObject);
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "UA", "EN", "RU" });
        dropdown.value = GetCurrentLanguageIndex();
        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
    }

    private void OnLanguageDropdownChanged(int value)
    {
        RuntimeLocalization localization = RuntimeLocalization.EnsureExists();
        localization.SetLanguage((AppLanguage)Mathf.Clamp(value, 0, 2));
        RefreshCharacterList();
        localization.ApplyToScene();
    }

    private int GetCurrentLanguageIndex()
    {
        return Mathf.Clamp((int)RuntimeLocalization.EnsureExists().CurrentLanguage, 0, 2);
    }

    private void AddLocalizedIgnore(GameObject target)
    {
        if (target != null && target.GetComponent<LocalizedIgnore>() == null)
            target.AddComponent<LocalizedIgnore>();
    }

    private Transform GetUserMenuParent()
    {
        if (userMenuRoot != null && userMenuRoot.parent != null)
            return userMenuRoot.parent;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            return canvas.transform;

        return transform.parent != null ? transform.parent : transform;
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

        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
                {
                    string exportPath = EnsureJsonExtension(paths[0]);
                    string json = JsonUtility.ToJson(saveManager.saveData, true);
                    File.WriteAllText(exportPath, json);
                    Debug.Log("DnD save file exported to: " + exportPath);
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            "",
            "Зберегти файл",
            "DndCharactersData.json"
        );
    }

    private void ImportFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter("JSON Files", ".json"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0])) return;

                try
                {
                    string importedJson = File.ReadAllText(paths[0]);
                    AppSaveData importedData = JsonUtility.FromJson<AppSaveData>(importedJson);
                    if (importedData != null && importedData.characters != null)
                    {
                        saveManager.saveData = importedData;
                        saveManager.NormalizeSaveData();
                        if (importedData.characters.Count > 0 &&
                            saveManager.GetCharacter(importedData.lastActiveCharacterId) == null)
                        {
                            importedData.lastActiveCharacterId = importedData.characters[0].id;
                        }

                        saveManager.SaveData();
                        RefreshCharacterList();
                        Debug.Log("DnD save file imported from: " + paths[0]);
                    }
                }
                catch (System.Exception exception)
                {
                    Debug.LogError("Could not import DnD save file: " + exception.Message);
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            "",
            "Виберіть файл JSON",
            "Вибрати"
        );
    }

    private void ExportSelectedCharacterFile()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        CharacterData characterCopy = JsonUtility.FromJson<CharacterData>(JsonUtility.ToJson(character));
        CharacterExportData exportData = new CharacterExportData { character = characterCopy };
        string fileName = MakeSafeFileName(character.characterName, "DnDCharacter") + ".dndchar";

        FileBrowser.SetDefaultFilter(".dndchar");
        FileBrowser.ShowSaveDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                string exportPath = EnsureExtension(paths[0], ".dndchar");
                File.WriteAllText(exportPath, JsonUtility.ToJson(exportData, true));
                Debug.Log("DnD character exported to: " + exportPath);
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            "",
            "Зберегти персонажа",
            fileName
        );
    }

    private void ImportCharacterFile()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        FileBrowser.SetFilters(true, new FileBrowser.Filter("DnD Character", ".dndchar", ".json"));
        FileBrowser.ShowLoadDialog(
            (paths) =>
            {
                if (paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                try
                {
                    string importedJson = File.ReadAllText(paths[0]);
                    CharacterExportData importedData = JsonUtility.FromJson<CharacterExportData>(importedJson);
                    CharacterData importedCharacter = importedData != null ? importedData.character : null;

                    if (importedCharacter == null)
                        importedCharacter = JsonUtility.FromJson<CharacterData>(importedJson);

                    if (importedCharacter == null)
                        return;

                    CharacterData characterCopy = JsonUtility.FromJson<CharacterData>(JsonUtility.ToJson(importedCharacter));
                    characterCopy.id = System.Guid.NewGuid().ToString();
                    characterCopy.characterName = MakeImportedCharacterName(saveManager, characterCopy.characterName);

                    saveManager.saveData.characters.Add(characterCopy);
                    saveManager.saveData.lastActiveCharacterId = characterCopy.id;
                    saveManager.NormalizeSaveData();
                    saveManager.SaveData();
                    RefreshCharacterList();
                    Debug.Log("DnD character imported from: " + paths[0]);
                }
                catch (System.Exception exception)
                {
                    Debug.LogError("Could not import DnD character file: " + exception.Message);
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            "",
            "Виберіть файл персонажа",
            "Вибрати"
        );
    }

    private string EnsureJsonExtension(string path)
    {
        return EnsureExtension(path, ".json");
    }

    private string EnsureExtension(string path, string extension)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        if (!extension.StartsWith("."))
            extension = "." + extension;

        return Path.GetExtension(path).Equals(extension, System.StringComparison.OrdinalIgnoreCase)
            ? path
            : path + extension;
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
                options.Add(string.IsNullOrWhiteSpace(character.characterName) ? "Новий персонаж" : character.characterName);
        }
        else
        {
            options.Add("Немає персонажів");
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
            oneCharacterDropdown.captionText.text = "Оберіть персонажа";

        if (oneCharacterTmpDropdown != null && oneCharacterTmpDropdown.captionText != null)
            oneCharacterTmpDropdown.captionText.text = "Оберіть персонажа";
    }

    private void ApplySelectedOneCharacterDropdownCaption()
    {
        CharacterData character = GetSelectedCharacterForExport();
        if (character == null)
            return;

        string label = string.IsNullOrWhiteSpace(character.characterName) ? "Новий персонаж" : character.characterName;

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

    private string MakeImportedCharacterName(DndSaveManager saveManager, string baseName)
    {
        baseName = string.IsNullOrWhiteSpace(baseName) ? "Імпортований персонаж" : baseName.Trim();
        if (!CharacterNameExists(saveManager, baseName))
            return baseName;

        int index = 2;
        string candidate;
        do
        {
            candidate = baseName + " (" + index + ")";
            index++;
        }
        while (CharacterNameExists(saveManager, candidate));

        return candidate;
    }

    private bool CharacterNameExists(DndSaveManager saveManager, string name)
    {
        if (saveManager == null || saveManager.saveData == null || saveManager.saveData.characters == null)
            return false;

        foreach (CharacterData character in saveManager.saveData.characters)
            if (character != null && string.Equals(character.characterName, name, System.StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private string MakeSafeFileName(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = fallback;

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');

        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    public void RefreshCharacterList()
    {
        RefreshOneCharacterDropdown();

        if (RefreshUserCreatedMenu())
            return;

        if (characterRowsContent != null && characterRowTemplate != null)
        {
            RefreshCharacterRows();
            return;
        }

        if (DndSaveManager.Instance == null || characterListContent == null)
            return;

        if (applyDefaultCharacterListLayout)
            EnsureCharacterListLayout();
        CacheCharacterButtonTemplate();
        DisableAutomaticContentLayout();
        if (characterButtonTemplate != null)
            characterButtonTemplate.SetActive(false);

        RectTransform templateRect = characterButtonTemplate != null
            ? characterButtonTemplate.GetComponent<RectTransform>()
            : null;

        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in characterListContent)
        {
            if (characterButtonTemplate != null && child.gameObject == characterButtonTemplate)
                continue;

            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
            Destroy(child);

        int buttonIndex = 0;
        RectTransform layoutSourceRect = templateRect;
        foreach (CharacterData character in DndSaveManager.Instance.saveData.characters)
        {
            GameObject btnObj = CreateCharacterButtonObject(character);
            btnObj.transform.SetParent(characterListContent, false);
            btnObj.SetActive(true);
            btnObj.transform.localScale = Vector3.one;

            RectTransform rectTransform = btnObj.GetComponent<RectTransform>();
            if (layoutSourceRect == null)
                layoutSourceRect = rectTransform;

            ApplyTemplateRectToClone(layoutSourceRect, rectTransform, buttonIndex);
            if (applyDefaultCharacterListLayout && rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 1f);
                rectTransform.anchorMax = new Vector2(1f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(0f, CharacterButtonHeight);
            }

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnCharacterSelected(character.id));
            }

            Transform deleteBtnTransform = btnObj.transform.Find("DeleteButton");
            if (deleteBtnTransform != null)
            {
                Button deleteBtn = deleteBtnTransform.GetComponent<Button>();
                if (deleteBtn != null)
                {
                    string characterId = character.id;
                    deleteBtn.onClick.RemoveAllListeners();
                    deleteBtn.onClick.AddListener(() => 
                    {
                        DndSaveManager.Instance.DeleteCharacter(characterId);
                        RefreshCharacterList();
                    });
                }
            }

            buttonIndex++;
        }

        ResizeContentForManualLayout(layoutSourceRect, buttonIndex);
    }

    private void WireUserCreatedMenu()
    {
        ScrollRect menuScroll = FindScrollRectInScene("menukart");
        if (menuScroll == null || menuScroll.content == null)
        {
            Button addButton = FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                addCharacterButton = addButton;

            return;
        }

        ConfigureUserMenuScroll(menuScroll);

        userMenuRoot = menuScroll.transform;
        userMenuContent = menuScroll.content;

        userCharacterButtonTemplate = FindButtonUnder(userMenuContent, "kartaPerson (1)");
        if (userCharacterButtonTemplate == null)
            userCharacterButtonTemplate = FindFirstDirectButtonInContent(userMenuContent);

        Button contentAddButton = FindButtonUnder(userMenuContent, "newpersonajbaton");
        if (contentAddButton != null)
            addCharacterButton = contentAddButton;
        else
        {
            Button addButton = FindButtonInScene("newpersonajbaton");
            if (addButton != null)
                addCharacterButton = addButton;
        }

        if (userCharacterButtonTemplate == null)
            Debug.LogWarning("MainMenuManager: cannot find character button template 'kartaPerson (1)' under menukart.");
        if (addCharacterButton == null)
            Debug.LogWarning("MainMenuManager: cannot find add button 'newpersonajbaton'.");
    }

    private void ConfigureUserMenuScroll(ScrollRect menuScroll)
    {
        menuScroll.horizontal = false;
        menuScroll.vertical = true;
        menuScroll.movementType = ScrollRect.MovementType.Clamped;
        menuScroll.inertia = true;
        menuScroll.decelerationRate = 0.08f;
        menuScroll.scrollSensitivity = 18f;
        menuScroll.horizontalNormalizedPosition = 0f;

        RectTransform contentRect = menuScroll.content;
        if (contentRect != null)
            contentRect.anchoredPosition = new Vector2(0f, contentRect.anchoredPosition.y);

        DisableAutomaticLayout(menuScroll.content);
    }

    private bool RefreshUserCreatedMenu()
    {
        WireUserCreatedMenu();
        if (userMenuContent == null || userCharacterButtonTemplate == null)
            return false;

        KeepAddButtonVisibleOutsideTemplate();
        SetTemplateActive(false);

        List<GameObject> rowsToDestroy = new List<GameObject>();
        Transform searchRoot = userMenuRoot != null ? userMenuRoot : userMenuContent;
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == searchRoot || child == userMenuContent)
                continue;

            if (IsTemplateOrTemplateChild(child))
                continue;

            if (child.name.StartsWith("CharacterMenuClone_"))
                rowsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject row in rowsToDestroy)
            Destroy(row);

        lastUserMenuRowRect = null;
        int index = 0;
        foreach (CharacterData character in DndSaveManager.Instance.saveData.characters)
        {
            CreateUserMenuCharacterRow(character, index);
            index++;
        }

        PositionAddButtonAfterRows();
        ResizeUserMenuContent(index);
        ScheduleAddButtonPositionRefresh();
        return true;
    }

    private void CreateUserMenuCharacterRow(CharacterData character, int index)
    {
        Button characterButton = CloneUserMenuButton(userCharacterButtonTemplate, "CharacterMenuClone_Row_" + index, index);
        if (characterButton == null)
            return;

        Button inventoryButton = FindButtonUnder(characterButton.transform, "spellbook");
        Button spellsButton = FindFirstButtonUnder(characterButton.transform, "invetory", "inventory");
        Button deleteButton = FindButtonUnder(characterButton.transform, "DeleteButton");

        Text nameText = FindCharacterNameText(characterButton);
        if (nameText != null)
            IgnoreLocalizationForDynamicText(nameText.gameObject);

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(character.characterName) ? "Новий персонаж" : character.characterName;

        string characterId = character.id;
        BindButton(characterButton, () => OnCharacterSelected(characterId));
        BindButton(inventoryButton, () => OnInventorySelected(characterId));
        BindButton(spellsButton, () => OnSpellbookSelected(characterId));
        BindButton(deleteButton, () =>
        {
            DndSaveManager.Instance.DeleteCharacter(characterId);
            RefreshCharacterList();
        });

    }

    private void IgnoreLocalizationForDynamicText(GameObject textObject)
    {
        if (textObject != null && textObject.GetComponent<LocalizedIgnore>() == null)
            textObject.AddComponent<LocalizedIgnore>();
    }

    private Text FindCharacterNameText(Button rowButton)
    {
        if (rowButton == null)
            return null;

        Transform legacyText = rowButton.transform.Find("Text (Legacy)");
        if (legacyText != null && legacyText.TryGetComponent(out Text legacyTextComponent))
            return legacyTextComponent;

        Transform text = rowButton.transform.Find("Text");
        if (text != null && text.TryGetComponent(out Text textComponent))
            return textComponent;

        Text[] texts = rowButton.GetComponentsInChildren<Text>(true);
        foreach (Text candidate in texts)
        {
            Button ownerButton = FindOwningButton(candidate.transform);
            if (ownerButton == rowButton)
                return candidate;
        }

        return texts.Length > 0 ? texts[0] : null;
    }

    private Button FindOwningButton(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            Button button = current.GetComponent<Button>();
            if (button != null)
                return button;

            current = current.parent;
        }

        return null;
    }

    private Button CloneUserMenuButton(Button template, string cloneName, int index)
    {
        if (template == null)
            return null;

        GameObject clone = Instantiate(template.gameObject, template.transform.parent, false);
        clone.name = cloneName;
        clone.SetActive(true);
        RemoveAddButtonsFromClone(clone.transform);

        RectTransform templateRect = template.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (templateRect != null && cloneRect != null)
        {
            cloneRect.anchorMin = templateRect.anchorMin;
            cloneRect.anchorMax = templateRect.anchorMax;
            cloneRect.pivot = templateRect.pivot;
            cloneRect.sizeDelta = templateRect.sizeDelta;
            cloneRect.localScale = templateRect.localScale;
            cloneRect.localRotation = templateRect.localRotation;

            float height = Mathf.Max(1f, templateRect.rect.height);
            cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + characterRowSpacing));
            lastUserMenuRowRect = cloneRect;
        }

        return clone.GetComponent<Button>();
    }

    private void SetTemplateActive(bool active)
    {
        if (userCharacterButtonTemplate != null)
            userCharacterButtonTemplate.gameObject.SetActive(active);
    }

    private bool IsTemplateOrTemplateChild(Transform child)
    {
        return IsSameOrChild(child, userCharacterButtonTemplate);
    }

    private bool IsSameOrChild(Transform child, Button button)
    {
        return button != null && (child == button.transform || child.IsChildOf(button.transform));
    }

    private void RemoveAddButtonsFromClone(Transform cloneRoot)
    {
        Button[] buttons = cloneRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
            if (button != null && NamesMatch(button.gameObject.name, "newpersonajbaton"))
                Destroy(button.gameObject);
    }

    private void KeepAddButtonVisibleOutsideTemplate()
    {
        if (addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null)
            return;

        Transform addTransform = addCharacterButton.transform;
        RectTransform addRect = addCharacterButton.GetComponent<RectTransform>();
        RectTransform templateRect = userCharacterButtonTemplate.GetComponent<RectTransform>();

        if (!hasAddButtonWorldOffsetFromTemplate && addRect != null && templateRect != null)
        {
            addButtonWorldOffsetFromTemplate = addRect.position - templateRect.position;
            hasAddButtonWorldOffsetFromTemplate = true;
        }

        if (addTransform.IsChildOf(userCharacterButtonTemplate.transform))
            addTransform.SetParent(userMenuContent, true);
    }

    private void PositionAddButtonAfterRows()
    {
        if (addCharacterButton == null || userCharacterButtonTemplate == null || userMenuContent == null || !hasAddButtonWorldOffsetFromTemplate)
            return;

        RectTransform addRect = addCharacterButton.GetComponent<RectTransform>();
        RectTransform templateRect = userCharacterButtonTemplate.GetComponent<RectTransform>();
        if (addRect == null || templateRect == null || addCharacterButton.transform.parent != userMenuContent)
            return;

        RectTransform targetRow = lastUserMenuRowRect != null ? lastUserMenuRowRect : templateRect;
        addRect.position = targetRow.position + addButtonWorldOffsetFromTemplate;
    }

    private void ScheduleAddButtonPositionRefresh()
    {
        if (!isActiveAndEnabled)
            return;

        if (addButtonPositionCoroutine != null)
            StopCoroutine(addButtonPositionCoroutine);

        addButtonPositionCoroutine = StartCoroutine(RefreshAddButtonPositionAtEndOfFrame());
    }

    private IEnumerator RefreshAddButtonPositionAtEndOfFrame()
    {
        yield return null;
        PositionAddButtonAfterRows();
        ResizeUserMenuContent(DndSaveManager.Instance != null && DndSaveManager.Instance.saveData != null ? DndSaveManager.Instance.saveData.characters.Count : 0);
        addButtonPositionCoroutine = null;
    }

    private void ResizeUserMenuContent(int rowCount)
    {
        RectTransform contentRect = userMenuContent as RectTransform;
        RectTransform templateRect = userCharacterButtonTemplate != null ? userCharacterButtonTemplate.GetComponent<RectTransform>() : null;
        if (contentRect == null || templateRect == null || rowCount <= 0)
            return;

        float height = Mathf.Max(1f, templateRect.rect.height);
        float addHeight = 0f;
        float addBottom = 0f;
        if (addCharacterButton != null && addCharacterButton.transform.parent == userMenuContent && addCharacterButton.TryGetComponent(out RectTransform addRect))
        {
            addHeight = Mathf.Max(1f, addRect.rect.height);
            addBottom = Mathf.Abs(addRect.anchoredPosition.y) + addHeight + 24f;
        }

        float bottom = Mathf.Abs(templateRect.anchoredPosition.y) + rowCount * height + Mathf.Max(0, rowCount) * characterRowSpacing + addHeight + 24f;
        bottom = Mathf.Max(bottom, addBottom);
        if (contentRect.sizeDelta.y < bottom)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, bottom);
    }

    private void OnCreateNewCharacterClicked()
    {
        if (Time.unscaledTime - lastCharacterCreateTime < 0.5f)
            return;

        lastCharacterCreateTime = Time.unscaledTime;

        CharacterData newChar = DndSaveManager.Instance.CreateNewCharacter();
        RefreshCharacterList();

        if (openCharacterAfterCreate)
            OnCharacterSelected(newChar.id);
    }

    private void OnCharacterSelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;
        
        SceneManager.LoadScene(characterSheetSceneName);
    }

    private void OnInventorySelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(inventorySceneName);
    }

    private void OnSpellbookSelected(string characterId)
    {
        if (!DndSaveManager.Instance.SetActiveCharacter(characterId))
            return;

        SceneManager.LoadScene(spellbookSceneName);
    }

    private void RefreshCharacterRows()
    {
        if (DndSaveManager.Instance == null)
            return;

        characterRowTemplate.SetActive(false);

        List<GameObject> rowsToDestroy = new List<GameObject>();
        foreach (Transform child in characterRowsContent)
        {
            if (child.gameObject == characterRowTemplate)
                continue;

            rowsToDestroy.Add(child.gameObject);
        }

        foreach (GameObject row in rowsToDestroy)
            Destroy(row);

        RectTransform templateRect = characterRowTemplate.GetComponent<RectTransform>();
        int rowIndex = 0;
        foreach (CharacterData character in DndSaveManager.Instance.saveData.characters)
        {
            GameObject row = Instantiate(characterRowTemplate, characterRowsContent, false);
            row.name = "CharacterRow_" + (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);
            row.SetActive(true);

            RectTransform rowRect = row.GetComponent<RectTransform>();
            ApplyRowTemplateRect(templateRect, rowRect, rowIndex);

            BindCharacterRow(row.transform, character);
            rowIndex++;
        }

        ResizeRowsContent(templateRect, rowIndex);
    }

    private void ApplyRowTemplateRect(RectTransform templateRect, RectTransform rowRect, int index)
    {
        if (templateRect == null || rowRect == null)
            return;

        rowRect.anchorMin = templateRect.anchorMin;
        rowRect.anchorMax = templateRect.anchorMax;
        rowRect.pivot = templateRect.pivot;
        rowRect.sizeDelta = templateRect.sizeDelta;
        rowRect.localRotation = templateRect.localRotation;
        rowRect.localScale = templateRect.localScale;

        float height = Mathf.Max(1f, templateRect.rect.height);
        rowRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + characterRowSpacing));
    }

    private void ResizeRowsContent(RectTransform templateRect, int rowCount)
    {
        RectTransform contentRect = characterRowsContent as RectTransform;
        if (contentRect == null || templateRect == null || rowCount <= 0)
            return;

        float height = Mathf.Max(1f, templateRect.rect.height);
        float topOffset = Mathf.Abs(templateRect.anchoredPosition.y);
        float requiredHeight = topOffset + rowCount * height + Mathf.Max(0, rowCount - 1) * characterRowSpacing + 24f;
        if (contentRect.sizeDelta.y < requiredHeight)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    private void BindCharacterRow(Transform row, CharacterData character)
    {
        Button characterButton = FindButton(row, "CharacterButton");
        Button inventoryButton = FindButton(row, "InventoryButton");
        Button deleteButton = FindButton(row, "DeleteButton");
        Button spellsButton = FindButton(row, "SpellsButton");

        Text nameText = characterButton != null
            ? characterButton.GetComponentInChildren<Text>(true)
            : row.GetComponentInChildren<Text>(true);
        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(character.characterName) ? "Новий персонаж" : character.characterName;

        string characterId = character.id;
        BindButton(characterButton, () => OnCharacterSelected(characterId));
        BindButton(inventoryButton, () => OnInventorySelected(characterId));
        BindButton(spellsButton, () => OnSpellbookSelected(characterId));
        BindButton(deleteButton, () =>
        {
            DndSaveManager.Instance.DeleteCharacter(characterId);
            RefreshCharacterList();
        });
    }

    private Button FindButton(Transform root, string name)
    {
        Transform found = root.Find(name);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private Button FindButtonInScene(string objectName)
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

    private ScrollRect FindScrollRectInScene(string objectName)
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

    private Button FindButtonUnder(Transform root, string objectName)
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

    private Button FindFirstDirectButtonInContent(Transform content)
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

    private Button FindFirstButtonUnder(Transform root, params string[] names)
    {
        foreach (string name in names)
        {
            Button button = FindButtonUnder(root, name);
            if (button != null)
                return button;
        }

        return null;
    }

    private bool NamesMatch(string actualName, string expectedName)
    {
        return string.Equals(actualName.Trim(), expectedName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSceneObject(GameObject gameObject)
    {
        return gameObject.scene.IsValid() && !string.IsNullOrEmpty(gameObject.scene.name);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void CacheCharacterButtonTemplate()
    {
        if (characterButtonTemplate != null || characterListContent == null)
            return;

        Transform template = characterListContent.Find("CharacterButtonTemplate");
        if (template != null)
            characterButtonTemplate = template.gameObject;
    }

    private void DisableAutomaticContentLayout()
    {
        if (characterListContent == null)
            return;

        DisableAutomaticLayout(characterListContent);
    }

    private void DisableAutomaticLayout(Transform content)
    {
        if (content == null)
            return;

        VerticalLayoutGroup layoutGroup = content.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
            layoutGroup.enabled = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;
    }

    private void ApplyTemplateRectToClone(RectTransform templateRect, RectTransform cloneRect, int index)
    {
        if (templateRect == null || cloneRect == null)
            return;

        cloneRect.anchorMin = templateRect.anchorMin;
        cloneRect.anchorMax = templateRect.anchorMax;
        cloneRect.pivot = templateRect.pivot;
        cloneRect.sizeDelta = templateRect.sizeDelta;
        cloneRect.localRotation = templateRect.localRotation;
        cloneRect.localScale = templateRect.localScale;

        float height = Mathf.Max(1f, templateRect.rect.height);
        cloneRect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -index * (height + characterButtonSpacing));
    }

    private void ResizeContentForManualLayout(RectTransform templateRect, int buttonCount)
    {
        RectTransform contentRect = characterListContent as RectTransform;
        if (contentRect == null || templateRect == null || buttonCount <= 0)
            return;

        float height = Mathf.Max(1f, templateRect.rect.height);
        float topOffset = Mathf.Abs(templateRect.anchoredPosition.y);
        float requiredHeight = topOffset + buttonCount * height + Mathf.Max(0, buttonCount - 1) * characterButtonSpacing + 24f;
        if (contentRect.sizeDelta.y < requiredHeight)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, requiredHeight);
    }

    private void EnsureCharacterListLayout()
    {
        if (characterListContent == null)
            return;

        RectTransform contentRect = characterListContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
        }

        VerticalLayoutGroup layoutGroup = characterListContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
            layoutGroup = characterListContent.gameObject.AddComponent<VerticalLayoutGroup>();

        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlWidth = false;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 12f;
        layoutGroup.padding = new RectOffset(12, 12, 84, 12);

        ContentSizeFitter fitter = characterListContent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = characterListContent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void EnsureScrollViewIsVisible()
    {
        ScrollRect scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
            return;

        VerticalLayoutGroup wrongLayoutGroup = GetComponent<VerticalLayoutGroup>();
        if (wrongLayoutGroup != null)
            wrongLayoutGroup.enabled = false;

        ContentSizeFitter wrongFitter = GetComponent<ContentSizeFitter>();
        if (wrongFitter != null)
            wrongFitter.enabled = false;

        RectTransform scrollRectTransform = transform as RectTransform;
        if (scrollRectTransform != null)
        {
            scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);

            Vector2 size = scrollRectTransform.sizeDelta;
            if (size.x < 320f)
                size.x = 620f;
            if (size.y < 320f)
                size.y = 980f;

            scrollRectTransform.sizeDelta = size;
        }

        if (scrollRect.viewport != null)
        {
            scrollRect.viewport.anchorMin = Vector2.zero;
            scrollRect.viewport.anchorMax = Vector2.one;
            scrollRect.viewport.offsetMin = Vector2.zero;
            scrollRect.viewport.offsetMax = Vector2.zero;
        }

        if (scrollRect.content == null && characterListContent is RectTransform contentRect)
            scrollRect.content = contentRect;
    }

    private GameObject CreateCharacterButtonObject(CharacterData character)
    {
        GameObject sourceButton = characterButtonTemplate != null
            ? characterButtonTemplate
            : useCharacterButtonPrefab && characterButtonPrefab != null
                ? characterButtonPrefab
                : null;

        GameObject btnObj = sourceButton != null
            ? Instantiate(sourceButton)
            : CreateFallbackCharacterButton();

        btnObj.name = "CharacterButton_" + (string.IsNullOrEmpty(character.characterName) ? character.id : character.characterName);

        if (btnObj.GetComponent<Button>() == null)
            btnObj.AddComponent<Button>();

        Image image = btnObj.GetComponent<Image>();
        if (image == null)
            image = btnObj.AddComponent<Image>();

        bool isActive = DndSaveManager.Instance != null &&
                        DndSaveManager.Instance.saveData.lastActiveCharacterId == character.id;
        if (applyDefaultCharacterButtonStyle)
        {
            image.color = isActive
                ? new Color(0.28f, 0.22f, 0.12f, 0.98f)
                : new Color(0.18f, 0.14f, 0.1f, 0.95f);
        }

        Text btnText = btnObj.GetComponentInChildren<Text>(true);
        if (btnText == null)
            btnText = CreateButtonText(btnObj.transform, "CharacterName", TextAnchor.MiddleLeft);

        btnText.text = string.IsNullOrEmpty(character.characterName) ? "Невідомий персонаж" : character.characterName;
        if (applyDefaultCharacterButtonStyle)
        {
            btnText.color = Color.white;
            btnText.fontSize = 28;
            btnText.resizeTextForBestFit = true;
            btnText.resizeTextMinSize = 16;
            btnText.resizeTextMaxSize = 28;
        }

        EnsureDeleteButton(btnObj.transform);
        return btnObj;
    }

    private GameObject CreateFallbackCharacterButton()
    {
        GameObject buttonObject = new GameObject("CharacterButton", typeof(RectTransform), typeof(Image), typeof(Button));
        CreateButtonText(buttonObject.transform, "CharacterName", TextAnchor.MiddleLeft);
        return buttonObject;
    }

    private Text CreateButtonText(Transform parent, string name, TextAnchor alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = new Vector2(24f, 8f);
        textRect.offsetMax = new Vector2(-90f, -8f);

        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        text.alignment = alignment;
        return text;
    }

    private void EnsureDeleteButton(Transform parent)
    {
        Transform existingDeleteButton = parent.Find("DeleteButton");
        if (existingDeleteButton != null)
            return;

        GameObject deleteObject = new GameObject("DeleteButton", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteObject.transform.SetParent(parent, false);

        RectTransform deleteRect = deleteObject.GetComponent<RectTransform>();
        deleteRect.anchorMin = new Vector2(1f, 0.5f);
        deleteRect.anchorMax = new Vector2(1f, 0.5f);
        deleteRect.pivot = new Vector2(0.5f, 0.5f);
        deleteRect.anchoredPosition = new Vector2(-45f, 0f);
        deleteRect.sizeDelta = new Vector2(58f, 58f);

        Image deleteImage = deleteObject.GetComponent<Image>();
        deleteImage.color = new Color(0.55f, 0.12f, 0.1f, 0.95f);

        Text deleteText = CreateButtonText(deleteObject.transform, "Text", TextAnchor.MiddleCenter);
        RectTransform textRect = deleteText.GetComponent<RectTransform>();
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        deleteText.text = "X";
        deleteText.color = Color.white;
        deleteText.fontSize = 28;
    }
}
