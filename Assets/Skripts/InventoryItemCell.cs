using System;
using System.Collections.Generic;
using SimpleFileBrowser;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemCell : MonoBehaviour
{
    private const int CategoryWeapon = 0;
    private const int CategoryArmor = 1;
    private const int CategoryBags = 2;
    private const int CategoryMagic = 3;
    private const int CategoryOther = 4;
    private const int CategoryCheger = 5;
    private const int CategoryCustom = 6;
    private const int CustomImageSize = 256;
    private const int CustomImageJpgQuality = 75;

    private InputField itemNameInput;
    private TMP_InputField itemNameTmpInput;
    private InputField itemDescriptionInput;
    private TMP_InputField itemDescriptionTmpInput;
    private Dropdown categoryDropdown;
    private TMP_Dropdown categoryTmpDropdown;
    private Dropdown weaponDropdown;
    private Dropdown armorDropdown;
    private Dropdown bagsDropdown;
    private Dropdown magicDropdown;
    private Dropdown otherDropdown;
    private Dropdown chegerDropdown;
    private TMP_Dropdown weaponTmpDropdown;
    private TMP_Dropdown armorTmpDropdown;
    private TMP_Dropdown bagsTmpDropdown;
    private TMP_Dropdown magicTmpDropdown;
    private TMP_Dropdown otherTmpDropdown;
    private TMP_Dropdown chegerTmpDropdown;
    private Button customImageButton;
    private Button exportButton;
    private Button importButton;
    private Button clearButton;
    private Image customImage;
    private GameObject customImagePanel;
    private Sprite customSprite;
    private Texture2D customTexture;
    private Sprite defaultCustomSprite;
    private Color defaultCustomColor = Color.white;
    private bool defaultCustomPreserveAspect;
    private string cellKey;
    private bool isLoading;

    [Serializable]
    public class InventoryItemExportData
    {
        public string itemName;
        public string itemDescription;
        public int category;
        public int weaponIndex;
        public int armorIndex;
        public int bagsIndex;
        public int magicIndex;
        public int otherIndex;
        public int chegerIndex;
        public string customImageBase64;
    }

    public void Initialize(int pageIndex, int cellIndex)
    {
        cellKey = "Inventory_Page_" + pageIndex + "_Cell_" + cellIndex;
        FindControls();
        BindControls();
        Load();
    }

    public void Save()
    {
        if (isLoading || string.IsNullOrEmpty(cellKey) || DndSaveManager.Instance == null)
            return;

        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();
        SaveToSceneData(sceneData, ReadCurrentData());
        DndSaveManager.Instance.SaveData();
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(cellKey) || DndSaveManager.Instance == null)
            return;

        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData(false);
        InventoryItemExportData data = ReadFromSceneData(sceneData);
        ApplyData(data, false);
    }

    public void ResetToDefaults(bool saveAfterReset)
    {
        InventoryItemExportData data = new InventoryItemExportData
        {
            category = CategoryWeapon,
            weaponIndex = 0,
            armorIndex = 0,
            bagsIndex = 0,
            magicIndex = 0,
            otherIndex = 0,
            chegerIndex = 0,
            customImageBase64 = ""
        };

        ApplyData(data, saveAfterReset);
    }

    private void FindControls()
    {
        itemNameInput = FindInput("itemNameInput");
        itemNameTmpInput = FindTmpInput("itemNameInput");
        itemDescriptionInput = FindInput("itemDescriptionInput");
        itemDescriptionTmpInput = FindTmpInput("itemDescriptionInput");

        categoryDropdown = FindDropdown("itemCategoryDropdown");
        categoryTmpDropdown = FindTmpDropdown("itemCategoryDropdown");
        weaponDropdown = FindDropdown("Dropdown weapon");
        armorDropdown = FindDropdown("Dropdown armor");
        bagsDropdown = FindDropdown("Dropdown bags");
        magicDropdown = FindDropdown("Dropdown magic");
        otherDropdown = FindDropdown("Dropdown other");
        chegerDropdown = FindDropdown("Dropdown cheger");
        weaponTmpDropdown = FindTmpDropdown("Dropdown weapon");
        armorTmpDropdown = FindTmpDropdown("Dropdown armor");
        bagsTmpDropdown = FindTmpDropdown("Dropdown bags");
        magicTmpDropdown = FindTmpDropdown("Dropdown magic");
        otherTmpDropdown = FindTmpDropdown("Dropdown other");
        chegerTmpDropdown = FindTmpDropdown("Dropdown cheger");

        customImageButton = FindButton("customItemImageButton");
        exportButton = FindButton("exportItemButton");
        importButton = FindButton("importItemButton");
        clearButton = FindButton("clearItemButton");
        customImage = FindImage("customItemImage");
        customImagePanel = FindChildGameObject("PanelForphoto");

        if (customImagePanel == null && customImage != null)
            customImagePanel = customImage.gameObject;

        if (customImage != null)
        {
            defaultCustomSprite = customImage.sprite;
            defaultCustomColor = customImage.color;
            defaultCustomPreserveAspect = customImage.preserveAspect;
        }

        EnsureCategoryOptions();
    }

    private void BindControls()
    {
        BindInput(itemNameInput);
        BindTmpInput(itemNameTmpInput);
        BindInput(itemDescriptionInput);
        BindTmpInput(itemDescriptionTmpInput);

        BindDropdown(categoryDropdown, OnCategoryChanged);
        BindTmpDropdown(categoryTmpDropdown, OnCategoryChanged);
        BindDropdown(weaponDropdown, OnAnyDropdownChanged);
        BindDropdown(armorDropdown, OnAnyDropdownChanged);
        BindDropdown(bagsDropdown, OnAnyDropdownChanged);
        BindDropdown(magicDropdown, OnAnyDropdownChanged);
        BindDropdown(otherDropdown, OnAnyDropdownChanged);
        BindDropdown(chegerDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(weaponTmpDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(armorTmpDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(bagsTmpDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(magicTmpDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(otherTmpDropdown, OnAnyDropdownChanged);
        BindTmpDropdown(chegerTmpDropdown, OnAnyDropdownChanged);

        if (customImageButton != null)
        {
            customImageButton.onClick.RemoveListener(SelectCustomImage);
            customImageButton.onClick.AddListener(SelectCustomImage);
        }

        if (exportButton != null)
        {
            exportButton.onClick.RemoveListener(ExportItem);
            exportButton.onClick.AddListener(ExportItem);
        }

        if (importButton != null)
        {
            importButton.onClick.RemoveListener(ImportItem);
            importButton.onClick.AddListener(ImportItem);
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(ClearItem);
            clearButton.onClick.AddListener(ClearItem);
        }
    }

    private void ClearItem()
    {
        ResetToDefaults(true);
    }

    private void OnCategoryChanged(int value)
    {
        ApplyCategoryVisibility(value);
        Save();
    }

    private void OnAnyDropdownChanged(int value)
    {
        Save();
    }

    private void SelectCustomImage()
    {
        NativeGallery.GetImageFromGallery(
            path =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                try
                {
                    Texture2D source = NativeGallery.LoadImageAtPath(path, 1024, false, false);
                    if (source == null)
                        return;

                    Texture2D resized = ResizeToSquare(source);
                    Destroy(source);
                    ApplyCustomTexture(resized);
                    Save();
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not load inventory item image: " + exception.Message);
                }
            },
            "Select item image",
            "image/*"
        );
    }

    private void ExportItem()
    {
        InventoryItemExportData data = ReadCurrentData();
        string fileName = MakeSafeFileName(string.IsNullOrWhiteSpace(data.itemName) ? "DnDItem" : data.itemName) + ".json";

        FileBrowser.SetDefaultFilter(".json");
        FileBrowser.ShowSaveDialog(
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                string exportPath = EnsureJsonExtension(paths[0]);
                FileBrowserHelpers.WriteTextToFile(exportPath, JsonUtility.ToJson(data, true));
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            GetDefaultFileBrowserPath(),
            fileName,
            "Save item",
            "Save"
        );
    }

    private void ImportItem()
    {
        FileBrowser.SetFilters(true, new FileBrowser.Filter("DnD Item JSON", ".json"));
        FileBrowser.ShowLoadDialog(
            paths =>
            {
                if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
                    return;

                try
                {
                    string json = FileBrowserHelpers.ReadTextFromFile(paths[0]);
                    InventoryItemExportData data = JsonUtility.FromJson<InventoryItemExportData>(json);
                    if (data == null)
                        return;

                    ApplyData(data, true);
                }
                catch (Exception exception)
                {
                    Debug.LogError("Could not import inventory item: " + exception.Message);
                }
            },
            () => { },
            FileBrowser.PickMode.Files,
            false,
            GetDefaultFileBrowserPath(),
            null,
            "Select item",
            "Select"
        );
    }

    private InventoryItemExportData ReadCurrentData()
    {
        return new InventoryItemExportData
        {
            itemName = GetInputText(itemNameInput, itemNameTmpInput),
            itemDescription = GetInputText(itemDescriptionInput, itemDescriptionTmpInput),
            category = GetDropdownValue(categoryDropdown, categoryTmpDropdown),
            weaponIndex = GetDropdownValue(weaponDropdown, weaponTmpDropdown),
            armorIndex = GetDropdownValue(armorDropdown, armorTmpDropdown),
            bagsIndex = GetDropdownValue(bagsDropdown, bagsTmpDropdown),
            magicIndex = GetDropdownValue(magicDropdown, magicTmpDropdown),
            otherIndex = GetDropdownValue(otherDropdown, otherTmpDropdown),
            chegerIndex = GetDropdownValue(chegerDropdown, chegerTmpDropdown),
            customImageBase64 = GetCurrentCustomImageBase64()
        };
    }

    private void ApplyData(InventoryItemExportData data, bool saveAfterApply)
    {
        if (data == null)
            data = new InventoryItemExportData();

        isLoading = true;
        try
        {
            SetInputText(itemNameInput, itemNameTmpInput, data.itemName);
            SetInputText(itemDescriptionInput, itemDescriptionTmpInput, data.itemDescription);
            SetDropdownValue(categoryDropdown, categoryTmpDropdown, data.category);
            SetDropdownValue(weaponDropdown, weaponTmpDropdown, data.weaponIndex);
            SetDropdownValue(armorDropdown, armorTmpDropdown, data.armorIndex);
            SetDropdownValue(bagsDropdown, bagsTmpDropdown, data.bagsIndex);
            SetDropdownValue(magicDropdown, magicTmpDropdown, data.magicIndex);
            SetDropdownValue(otherDropdown, otherTmpDropdown, data.otherIndex);
            SetDropdownValue(chegerDropdown, chegerTmpDropdown, data.chegerIndex);
            ApplyCustomImageBase64(data.customImageBase64);
            ApplyCategoryVisibility(data.category);
        }
        finally
        {
            isLoading = false;
        }

        if (saveAfterApply)
            Save();
    }

    private void SaveToSceneData(CharacterSceneData sceneData, InventoryItemExportData data)
    {
        if (sceneData == null || data == null)
            return;

        sceneData.SetString(cellKey + "_Name", data.itemName ?? "");
        sceneData.SetString(cellKey + "_Description", data.itemDescription ?? "");
        sceneData.SetInt(cellKey + "_Category", data.category);
        sceneData.SetInt(cellKey + "_Weapon", data.weaponIndex);
        sceneData.SetInt(cellKey + "_Armor", data.armorIndex);
        sceneData.SetInt(cellKey + "_Bags", data.bagsIndex);
        sceneData.SetInt(cellKey + "_Magic", data.magicIndex);
        sceneData.SetInt(cellKey + "_Other", data.otherIndex);
        sceneData.SetInt(cellKey + "_Cheger", data.chegerIndex);

        if (string.IsNullOrWhiteSpace(data.customImageBase64))
            sceneData.DeleteString(cellKey + "_CustomImage");
        else
            sceneData.SetString(cellKey + "_CustomImage", data.customImageBase64);
    }

    private InventoryItemExportData ReadFromSceneData(CharacterSceneData sceneData)
    {
        if (sceneData == null)
            return new InventoryItemExportData();

        return new InventoryItemExportData
        {
            itemName = sceneData.GetString(cellKey + "_Name", ""),
            itemDescription = sceneData.GetString(cellKey + "_Description", ""),
            category = sceneData.GetInt(cellKey + "_Category", 0),
            weaponIndex = sceneData.GetInt(cellKey + "_Weapon", 0),
            armorIndex = sceneData.GetInt(cellKey + "_Armor", 0),
            bagsIndex = sceneData.GetInt(cellKey + "_Bags", 0),
            magicIndex = sceneData.GetInt(cellKey + "_Magic", 0),
            otherIndex = sceneData.GetInt(cellKey + "_Other", 0),
            chegerIndex = sceneData.GetInt(cellKey + "_Cheger", 0),
            customImageBase64 = sceneData.GetString(cellKey + "_CustomImage", "")
        };
    }

    private void ApplyCategoryVisibility(int category)
    {
        SetDropdownVisible(weaponDropdown, weaponTmpDropdown, category == CategoryWeapon);
        SetDropdownVisible(armorDropdown, armorTmpDropdown, category == CategoryArmor);
        SetDropdownVisible(bagsDropdown, bagsTmpDropdown, category == CategoryBags);
        SetDropdownVisible(magicDropdown, magicTmpDropdown, category == CategoryMagic);
        SetDropdownVisible(otherDropdown, otherTmpDropdown, category == CategoryOther);
        SetDropdownVisible(chegerDropdown, chegerTmpDropdown, category == CategoryCheger);

        if (customImagePanel != null)
            customImagePanel.SetActive(category == CategoryCustom);
    }

    private void EnsureCategoryOptions()
    {
        string[] labels =
        {
            "Зброя",
            "Броня",
            "Сумки",
            "Магія",
            "Інше",
            "Скарби",
            "Своя картинка"
        };

        if (categoryDropdown != null && ShouldReplaceOptions(categoryDropdown.options.Count, GetOptionText(categoryDropdown, 0)))
        {
            categoryDropdown.ClearOptions();
            categoryDropdown.AddOptions(new List<string>(labels));
            categoryDropdown.RefreshShownValue();
        }

        if (categoryTmpDropdown != null && ShouldReplaceOptions(categoryTmpDropdown.options.Count, GetOptionText(categoryTmpDropdown, 0)))
        {
            categoryTmpDropdown.ClearOptions();
            categoryTmpDropdown.AddOptions(new List<string>(labels));
            categoryTmpDropdown.RefreshShownValue();
        }
    }

    private bool ShouldReplaceOptions(int optionCount, string firstOptionText)
    {
        return optionCount < 7 || IsGeneratedOptionText(firstOptionText);
    }

    private string GetOptionText(Dropdown dropdown, int index)
    {
        return dropdown != null && index >= 0 && index < dropdown.options.Count ? dropdown.options[index].text : "";
    }

    private string GetOptionText(TMP_Dropdown dropdown, int index)
    {
        return dropdown != null && index >= 0 && index < dropdown.options.Count ? dropdown.options[index].text : "";
    }

    private bool IsGeneratedOptionText(string text)
    {
        return !string.IsNullOrWhiteSpace(text) &&
               text.Trim().StartsWith("Option ", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyCustomTexture(Texture2D texture)
    {
        if (customImage == null || texture == null)
            return;

        ClearRuntimeCustomImage();
        customTexture = texture;
        customSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        customImage.sprite = customSprite;
        customImage.color = Color.white;
        customImage.type = Image.Type.Simple;
        customImage.preserveAspect = false;
        customImage.SetAllDirty();
    }

    private void ApplyCustomImageBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            RestoreDefaultCustomImage();
            return;
        }

        try
        {
            byte[] bytes = Convert.FromBase64String(base64);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(bytes))
                ApplyCustomTexture(texture);
            else
                Destroy(texture);
        }
        catch
        {
            RestoreDefaultCustomImage();
        }
    }

    private string GetCurrentCustomImageBase64()
    {
        if (customTexture == null)
            return "";

        return Convert.ToBase64String(customTexture.EncodeToJPG(CustomImageJpgQuality));
    }

    private void RestoreDefaultCustomImage()
    {
        if (customImage == null)
            return;

        ClearRuntimeCustomImage();
        customImage.sprite = defaultCustomSprite;
        customImage.color = defaultCustomColor;
        customImage.preserveAspect = defaultCustomPreserveAspect;
    }

    private void ClearRuntimeCustomImage()
    {
        if (customSprite != null)
        {
            Destroy(customSprite);
            customSprite = null;
        }

        if (customTexture != null)
        {
            Destroy(customTexture);
            customTexture = null;
        }
    }

    private Texture2D ResizeToSquare(Texture2D source)
    {
        Texture2D result = new Texture2D(CustomImageSize, CustomImageSize, TextureFormat.RGB24, false);
        Color[] pixels = new Color[CustomImageSize * CustomImageSize];

        for (int y = 0; y < CustomImageSize; y++)
        {
            float sourceY = CustomImageSize == 1 ? 0f : (float)y / (CustomImageSize - 1);
            for (int x = 0; x < CustomImageSize; x++)
            {
                float sourceX = CustomImageSize == 1 ? 0f : (float)x / (CustomImageSize - 1);
                pixels[y * CustomImageSize + x] = source.GetPixelBilinear(sourceX, sourceY);
            }
        }

        result.SetPixels(pixels);
        result.Apply(false, false);
        return result;
    }

    private void SetDropdownVisible(Dropdown dropdown, TMP_Dropdown tmpDropdown, bool visible)
    {
        if (dropdown != null)
            dropdown.gameObject.SetActive(visible);
        if (tmpDropdown != null)
            tmpDropdown.gameObject.SetActive(visible);
    }

    private void BindInput(InputField input)
    {
        if (input == null)
            return;

        input.onEndEdit.RemoveListener(OnInputChanged);
        input.onEndEdit.AddListener(OnInputChanged);
    }

    private void BindTmpInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.onEndEdit.RemoveListener(OnInputChanged);
        input.onEndEdit.AddListener(OnInputChanged);
    }

    private void BindDropdown(Dropdown dropdown, UnityEngine.Events.UnityAction<int> callback)
    {
        if (dropdown == null)
            return;

        dropdown.onValueChanged.RemoveListener(callback);
        dropdown.onValueChanged.AddListener(callback);
    }

    private void BindTmpDropdown(TMP_Dropdown dropdown, UnityEngine.Events.UnityAction<int> callback)
    {
        if (dropdown == null)
            return;

        dropdown.onValueChanged.RemoveListener(callback);
        dropdown.onValueChanged.AddListener(callback);
    }

    private void OnInputChanged(string value)
    {
        Save();
    }

    private string GetInputText(InputField input, TMP_InputField tmpInput)
    {
        if (input != null)
            return input.text;
        return tmpInput != null ? tmpInput.text : "";
    }

    private void SetInputText(InputField input, TMP_InputField tmpInput, string value)
    {
        if (input != null)
            input.SetTextWithoutNotify(value ?? "");
        if (tmpInput != null)
            tmpInput.SetTextWithoutNotify(value ?? "");
    }

    private int GetDropdownValue(Dropdown dropdown, TMP_Dropdown tmpDropdown)
    {
        if (dropdown != null)
            return dropdown.value;
        return tmpDropdown != null ? tmpDropdown.value : 0;
    }

    private void SetDropdownValue(Dropdown dropdown, TMP_Dropdown tmpDropdown, int value)
    {
        if (dropdown != null)
        {
            dropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, dropdown.options.Count - 1)));
            dropdown.RefreshShownValue();
        }

        if (tmpDropdown != null)
        {
            tmpDropdown.SetValueWithoutNotify(Mathf.Clamp(value, 0, Mathf.Max(0, tmpDropdown.options.Count - 1)));
            tmpDropdown.RefreshShownValue();
        }
    }

    private InputField FindInput(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<InputField>() : null;
    }

    private TMP_InputField FindTmpInput(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<TMP_InputField>() : null;
    }

    private Dropdown FindDropdown(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<Dropdown>() : null;
    }

    private TMP_Dropdown FindTmpDropdown(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<TMP_Dropdown>() : null;
    }

    private Button FindButton(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private Image FindImage(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private GameObject FindChildGameObject(string objectName)
    {
        Transform child = FindChild(objectName);
        return child != null ? child.gameObject : null;
    }

    private Transform FindChild(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
            if (child != null && NameMatches(child.name, objectName))
                return child;

        return null;
    }

    private string EnsureJsonExtension(string path)
    {
        return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? path : path + ".json";
    }

    private string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "DnDItem";

        foreach (char invalidChar in System.IO.Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');

        return value.Trim();
    }

    private string GetDefaultFileBrowserPath()
    {
        string[] candidates =
        {
            "/storage/emulated/0/Download",
            "/sdcard/Download",
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        foreach (string candidate in candidates)
            if (!string.IsNullOrEmpty(candidate) && System.IO.Directory.Exists(candidate))
                return candidate;

        return null;
    }

    private bool NameMatches(string actualName, string expectedName)
    {
        return GetBaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetBaseName(string name)
    {
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }
}
