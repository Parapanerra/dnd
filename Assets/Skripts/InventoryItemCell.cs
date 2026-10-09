using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemCell : MonoBehaviour
{
    private const int CategoryWeapon = AppConfig.Inventory.WeaponCategory;
    private const int CategoryArmor = AppConfig.Inventory.ArmorCategory;
    private const int CategoryBags = AppConfig.Inventory.BagsCategory;
    private const int CategoryMagic = AppConfig.Inventory.MagicCategory;
    private const int CategoryOther = AppConfig.Inventory.OtherCategory;
    private const int CategoryCheger = AppConfig.Inventory.ChegerCategory;
    private const int CategoryCustom = AppConfig.Inventory.CustomCategory;
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
    private GameObject customImagePanel;
    private InventoryItemImageController imageController;
    private string cellKey;
    private bool isLoading;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private InventoryItemTransferPanel transferPanel;
    internal string CellKey => cellKey;
    internal DndSaveManager SaveManager => saveManager;

    public void Initialize(int pageIndex, int cellIndex, DndSaveManager dataManager, RuntimeLocalization textCatalog)
    {
        saveManager = dataManager;
        localization = textCatalog;
        cellKey = "Inventory_Page_" + pageIndex + "_Cell_" + cellIndex;
        FindControls();
        BindControls();
        Load();
    }

    public void Save()
    {
        if (isLoading || string.IsNullOrEmpty(cellKey) || saveManager == null)
            return;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData();
        InventoryItemSerializer.WriteScene(sceneData, cellKey, ReadCurrentData());
        saveManager.RequestSaveData();
    }

    public void Load()
    {
        if (string.IsNullOrEmpty(cellKey) || saveManager == null)
            return;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData(false);
        InventoryItemExportData data = InventoryItemSerializer.ReadScene(sceneData, cellKey);
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

    public void RefreshLocalization()
    {
        if (localization == null)
            return;

        EnsureCategoryOptions();
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
        if (transferPanel == null)
            transferPanel = new InventoryItemTransferPanel();
        transferPanel.Initialize(this, exportButton, importButton);
        Image image = FindImage("customItemImage");
        customImagePanel = FindChildGameObject("PanelForphoto");
        if (customImagePanel == null && image != null)
            customImagePanel = image.gameObject;

        imageController = GetComponent<InventoryItemImageController>();
        if (imageController == null)
            imageController = gameObject.AddComponent<InventoryItemImageController>();
        imageController.Initialize(image);

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

        transferPanel.BindButtons();

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
        RefreshCategoryShownValue();
        Save();
    }

    private void OnAnyDropdownChanged(int value)
    {
        Save();
    }

    private void SelectCustomImage()
    {
        if (imageController != null)
            imageController.SelectFromGallery(Save);
    }

    internal InventoryItemExportData ReadCurrentData()
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
            customImageBase64 = imageController != null ? imageController.GetBase64() : ""
        };
    }

    internal void ApplyData(InventoryItemExportData data, bool saveAfterApply)
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
            if (imageController != null)
                imageController.ApplyBase64(data.customImageBase64);
            ApplyCategoryVisibility(data.category);
            RefreshCategoryShownValue();
        }
        finally
        {
            isLoading = false;
        }

        if (saveAfterApply)
            Save();
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
            Localize("Зброя"),
            Localize("Броня"),
            Localize("Сумки"),
            Localize("Магія"),
            Localize("Інше"),
            Localize("Скарби"),
            Localize("Своя картинка")
        };

        if (categoryDropdown != null)
        {
            int currentValue = categoryDropdown.value;
            categoryDropdown.ClearOptions();
            categoryDropdown.AddOptions(new List<string>(labels));
            categoryDropdown.SetValueWithoutNotify(Mathf.Clamp(currentValue, 0, categoryDropdown.options.Count - 1));
            categoryDropdown.RefreshShownValue();
        }

        if (categoryTmpDropdown != null)
        {
            int currentValue = categoryTmpDropdown.value;
            categoryTmpDropdown.ClearOptions();
            categoryTmpDropdown.AddOptions(new List<string>(labels));
            categoryTmpDropdown.SetValueWithoutNotify(Mathf.Clamp(currentValue, 0, categoryTmpDropdown.options.Count - 1));
            categoryTmpDropdown.RefreshShownValue();
        }
    }

    private string Localize(string source)
    {
        return localization.Translate(source);
    }

    private void RefreshCategoryShownValue()
    {
        if (categoryDropdown != null)
            categoryDropdown.RefreshShownValue();

        if (categoryTmpDropdown != null)
            categoryTmpDropdown.RefreshShownValue();
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
            if (child != null && SceneObjectName.Matches(child.name, objectName))
                return child;

        return null;
    }

}
