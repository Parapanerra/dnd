using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemCell : MonoBehaviour
{
    private const int CategoryWeapon = AppConfig.Inventory.WeaponCategory;
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
    private InventoryItemCategoryView categoryView;
    private InventoryItemImageController imageController;
    private string cellKey;
    private bool isLoading;
    private DndSaveManager saveManager;
    private RuntimeLocalization localization;
    private InventoryItemTransferPanel transferPanel;
    private readonly InventoryItemSerializer serializer = new InventoryItemSerializer();
    private Transform[] controls;
    internal string CellKey => cellKey;
    internal DndSaveManager SaveManager => saveManager;

    internal void Initialize(int pageIndex, int cellIndex, DndSaveManager dataManager, RuntimeLocalization textCatalog)
    {
        saveManager = dataManager;
        localization = textCatalog;
        cellKey = "Inventory_Page_" + pageIndex + "_Cell_" + cellIndex;
        FindControls();
        BindControls();
        Load();
    }

    internal void Save()
    {
        if (isLoading || string.IsNullOrEmpty(cellKey) || saveManager == null)
            return;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData();
        serializer.WriteScene(sceneData, cellKey, ReadCurrentData());
        saveManager.RequestSaveData();
    }

    internal void Load()
    {
        if (string.IsNullOrEmpty(cellKey) || saveManager == null)
            return;

        CharacterSceneData sceneData = saveManager.GetActiveSceneData(false);
        InventoryItemExportData data = serializer.ReadScene(sceneData, cellKey);
        ApplyData(data, false);
    }

    internal void ResetToDefaults(bool saveAfterReset)
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

    internal void RefreshLocalization()
    {
        if (localization == null)
            return;

        categoryView.RefreshOptions();
    }

    private void FindControls()
    {
        controls = GetComponentsInChildren<Transform>(true);
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
        transferPanel.Initialize(this, exportButton, importButton, localization);
        Image image = FindImage("customItemImage");
        GameObject customImagePanel = FindChildGameObject("PanelForphoto");
        if (customImagePanel == null && image != null)
            customImagePanel = image.gameObject;

        imageController = GetComponent<InventoryItemImageController>();
        if (imageController == null)
            imageController = gameObject.AddComponent<InventoryItemImageController>();
        imageController.Initialize(image);

        categoryView = new InventoryItemCategoryView(
            categoryDropdown, categoryTmpDropdown,
            new[] { weaponDropdown, armorDropdown, bagsDropdown, magicDropdown, otherDropdown, chegerDropdown },
            new[] { weaponTmpDropdown, armorTmpDropdown, bagsTmpDropdown, magicTmpDropdown, otherTmpDropdown, chegerTmpDropdown },
            customImagePanel, localization);
        categoryView.RefreshOptions();
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
        categoryView.ShowCategory(value);
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
            categoryView.ShowCategory(data.category);
        }
        finally
        {
            isLoading = false;
        }

        if (saveAfterApply)
            Save();
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
        foreach (Transform child in controls)
            if (child != null && SceneObjectName.Matches(child.name, objectName))
                return child;

        return null;
    }

}
