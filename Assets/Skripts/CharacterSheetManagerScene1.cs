using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using SimpleFileBrowser;
using System.IO;
using TMPro;
using System;

public class CharacterSheetManagerScene1 : MonoBehaviour
{
    private const string DropdownKeyPrefix = "Dropdown_";
    private const string TmpDropdownKeyPrefix = "TMPDropdown_";
    private const string ToggleKeyPrefix = "Toggle_";
    private const string RestResourceKeyPrefix = "RestResource_";
    private const string CharacterNameObjectName = "personajName";
    private const string CharacterNameFieldPath = "playerInfo/inputPises/personajName";
    private const string LegacyCharacterNameFieldPath = "playerInfo/inputPises/mmpises1";

    [Header("UI References")]
    public List<InputField> inputFields;
    public List<Toggle> toggles;
    public List<Slider> sliders;
    public List<Dropdown> dropdowns;
    
    [Header("Buttons")]
    public Button backToMenuButton;
    public Button deleteCharacterButton;
    public Button resetButton;

    private CharacterData currentCharacter;
    private CharacterSceneData currentSceneData;
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private string characterId;
    private string sceneName;
    private InputField characterNameInputField;
    private TMP_InputField characterNameTmpInputField;
    private bool isLoadingSceneData;
    private List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
    private List<Button> resetButtons = new List<Button>();

    private void Start()
    {
        DndSaveManager saveManager = DndSaveManager.EnsureExists();

        currentCharacter = saveManager.EnsureActiveCharacter();
        characterId = currentCharacter.id;
        sceneName = saveManager.GetActiveSceneDataName();
        currentSceneData = currentCharacter.GetSceneData(sceneName);
        CacheSceneControls();
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        EnsureCharacterPortraitManager();

        if (currentCharacter == null)
        {
            Debug.LogError("Could not find or create active character.");
            return;
        }

        LoadCharacterDataToUI();
        DeathSaveToggleSequence.ConfigureScene();
        RuntimeLocalization.EnsureExists().ApplyToScene();

        SubscribeToUIEvents();

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("menu"));

        if (deleteCharacterButton != null)
        {
            deleteCharacterButton.onClick.AddListener(() => 
            {
                DndSaveManager.Instance.DeleteCharacter(characterId);
                SceneManager.LoadScene("menu");
            });
        }

        BindResetButtons();
    }

    #region ЗБЕРЕЖЕННЯ / ЗАВАНТАЖЕННЯ (ООП)

    public void SaveCharacterData()
    {
        if (isLoadingSceneData)
            return;

        if (DndSaveManager.Instance != null)
        {
            currentCharacter = DndSaveManager.Instance.GetCharacter(characterId);
            currentSceneData = DndSaveManager.Instance.GetSceneDataForCharacter(characterId, sceneName);
        }

        if (currentCharacter == null || currentSceneData == null) return;

        currentSceneData.inputData.Clear();
        foreach (var input in inputFields)
            currentSceneData.inputData.Add(input != null ? input.text : "");

        foreach (var input in tmpInputFields)
            currentSceneData.inputData.Add(input != null ? input.text : "");

        SaveCharacterNameIfPossible();
        SaveSharedCharacterInputs();

        currentSceneData.toggleData.Clear();
        foreach (var toggle in toggles)
        {
            bool isOn = toggle != null && toggle.isOn;
            currentSceneData.toggleData.Add(isOn);
            if (toggle != null)
                currentSceneData.SetInt(ToggleKeyPrefix + GetControlPath(toggle.transform), isOn ? 1 : 0);
        }

        currentSceneData.sliderData.Clear();
        foreach (var slider in sliders)
            currentSceneData.sliderData.Add(slider != null ? slider.value : 0f);

        currentSceneData.dropdownData.Clear();
        foreach (var dropdown in dropdowns)
        {
            currentSceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                currentSceneData.SetInt(DropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        foreach (var dropdown in tmpDropdowns)
        {
            currentSceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                currentSceneData.SetInt(TmpDropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        SaveRestResourceMarkers();

        DndSaveManager.Instance.SaveData();
    }

    private void LoadCharacterDataToUI()
    {
        if (currentCharacter == null || currentSceneData == null) return;

        MigrateLegacySceneDataIfNeeded();

        isLoadingSceneData = true;
        try
        {
            int dataIndex = 0;

            for (int i = 0; i < inputFields.Count; i++, dataIndex++)
                if (inputFields[i] != null)
                    inputFields[i].SetTextWithoutNotify(dataIndex < currentSceneData.inputData.Count ? currentSceneData.inputData[dataIndex] : "");

            for (int i = 0; i < tmpInputFields.Count; i++, dataIndex++)
                if (tmpInputFields[i] != null)
                    tmpInputFields[i].SetTextWithoutNotify(dataIndex < currentSceneData.inputData.Count ? currentSceneData.inputData[dataIndex] : "");

            for (int i = 0; i < toggles.Count; i++)
                if (toggles[i] != null)
                {
                    string key = ToggleKeyPrefix + GetControlPath(toggles[i].transform);
                    bool value = currentSceneData.HasInt(key)
                        ? currentSceneData.GetInt(key) != 0
                        : i < currentSceneData.toggleData.Count && currentSceneData.toggleData[i];
                    toggles[i].SetIsOnWithoutNotify(value);
                }

            for (int i = 0; i < sliders.Count; i++)
                if (sliders[i] != null)
                    sliders[i].SetValueWithoutNotify(i < currentSceneData.sliderData.Count ? currentSceneData.sliderData[i] : 0f);

            for (int i = 0; i < dropdowns.Count; i++)
                if (dropdowns[i] != null)
                {
                    string key = DropdownKeyPrefix + GetControlPath(dropdowns[i].transform);
                    int value = currentSceneData.HasInt(key)
                        ? currentSceneData.GetInt(key)
                        : i < currentSceneData.dropdownData.Count ? currentSceneData.dropdownData[i] : 0;
                    dropdowns[i].SetValueWithoutNotify(value);
                    dropdowns[i].RefreshShownValue();
                }

            int tmpDropdownOffset = dropdowns.Count;
            for (int i = 0; i < tmpDropdowns.Count; i++)
                if (tmpDropdowns[i] != null)
                {
                    string key = TmpDropdownKeyPrefix + GetControlPath(tmpDropdowns[i].transform);
                    int value = currentSceneData.HasInt(key)
                        ? currentSceneData.GetInt(key)
                        : i + tmpDropdownOffset < currentSceneData.dropdownData.Count ? currentSceneData.dropdownData[i + tmpDropdownOffset] : 0;
                    tmpDropdowns[i].SetValueWithoutNotify(value);
                    tmpDropdowns[i].RefreshShownValue();
                }

            LoadCharacterNameToUi();
            LoadSharedCharacterInputs();
        }
        finally
        {
            isLoadingSceneData = false;
        }

        RefreshDropdownDrivenUi();
        RefreshToggleDrivenPanels();
        RefreshHealthBars();
    }

    private void RefreshDropdownDrivenUi()
    {
        DropdownManager[] dropdownManagers = FindObjectsByType<DropdownManager>(FindObjectsInactive.Include);
        foreach (DropdownManager manager in dropdownManagers)
            if (manager != null)
                manager.RefreshAll();

        DropdownVisibilityController[] visibilityControllers = FindObjectsByType<DropdownVisibilityController>(FindObjectsInactive.Include);
        foreach (DropdownVisibilityController controller in visibilityControllers)
            if (controller != null)
                controller.RefreshVisibility();
    }

    private void RefreshToggleDrivenPanels()
    {
        PanelToggleManager[] panelToggleManagers = FindObjectsByType<PanelToggleManager>(FindObjectsInactive.Include);
        foreach (PanelToggleManager manager in panelToggleManagers)
            if (manager != null)
                manager.RefreshPanels();
    }

    private void RefreshHealthBars()
    {
        HealthBar[] healthBars = FindObjectsByType<HealthBar>(FindObjectsInactive.Include);
        foreach (HealthBar healthBar in healthBars)
            if (healthBar != null)
                healthBar.RefreshHealthFromData();

        HealthBar1[] healthBarOnes = FindObjectsByType<HealthBar1>(FindObjectsInactive.Include);
        foreach (HealthBar1 healthBar in healthBarOnes)
            if (healthBar != null)
                healthBar.RefreshHealthFromData();
    }

    public void SwitchSceneData(string newSceneName)
    {
        if (string.IsNullOrWhiteSpace(newSceneName) || DndSaveManager.Instance == null)
            return;

        SaveCharacterData();

        currentCharacter = DndSaveManager.Instance.GetCharacter(characterId);
        if (currentCharacter == null)
            currentCharacter = DndSaveManager.Instance.EnsureActiveCharacter();

        characterId = currentCharacter.id;
        sceneName = newSceneName;
        DndSaveManager.Instance.SetActiveSceneDataName(sceneName);
        currentSceneData = currentCharacter.GetSceneData(sceneName);

        LoadCharacterDataToUI();
        RuntimeLocalization.EnsureExists().ApplyToScene();
    }

    private void MigrateLegacySceneDataIfNeeded()
    {
        if (currentSceneData.inputData.Count > 0 ||
            currentSceneData.toggleData.Count > 0 ||
            currentSceneData.sliderData.Count > 0 ||
            currentSceneData.dropdownData.Count > 0)
        {
            return;
        }

        currentSceneData.inputData.AddRange(currentCharacter.inputData);
        currentSceneData.toggleData.AddRange(currentCharacter.toggleData);
        currentSceneData.sliderData.AddRange(currentCharacter.sliderData);
        currentSceneData.dropdownData.AddRange(currentCharacter.dropdownData);
    }

    private void CacheSceneControls()
    {
        inputFields = new List<InputField>(FindObjectsByType<InputField>(FindObjectsInactive.Include));
        tmpInputFields = new List<TMP_InputField>(FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include));
        toggles = new List<Toggle>(FindObjectsByType<Toggle>(FindObjectsInactive.Include));
        sliders = new List<Slider>(FindObjectsByType<Slider>(FindObjectsInactive.Include));
        dropdowns = new List<Dropdown>(FindObjectsByType<Dropdown>(FindObjectsInactive.Include));
        tmpDropdowns = new List<TMP_Dropdown>(FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include));
        resetButtons = new List<Button>(FindObjectsByType<Button>(FindObjectsInactive.Include));

        inputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpInputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        toggles.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        sliders.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        dropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpDropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        resetButtons.RemoveAll(button => !IsResetButton(button));

        CacheCharacterNameField();
    }

    private void CacheCharacterNameField()
    {
        characterNameInputField = null;
        characterNameTmpInputField = null;

        if (TryCacheCharacterNameFieldByObjectName())
            return;

        InputField exactInput = null;
        TMP_InputField exactTmpInput = null;

        foreach (InputField input in inputFields)
        {
            if (input == null)
                continue;

            if (IsExactCharacterNameField(input.transform))
            {
                exactInput = input;
                break;
            }

            if (characterNameInputField == null && IsCharacterNameField(input.transform))
            {
                characterNameInputField = input;
            }
        }

        if (exactInput != null)
        {
            characterNameInputField = exactInput;
            return;
        }

        foreach (TMP_InputField input in tmpInputFields)
        {
            if (input == null)
                continue;

            if (IsExactCharacterNameField(input.transform))
            {
                exactTmpInput = input;
                break;
            }

            if (characterNameTmpInputField == null && IsCharacterNameField(input.transform))
            {
                characterNameTmpInputField = input;
            }
        }

        if (exactTmpInput != null)
        {
            characterNameInputField = null;
            characterNameTmpInputField = exactTmpInput;
        }
    }

    private bool TryCacheCharacterNameFieldByObjectName()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
        {
            if (transform.name != CharacterNameObjectName)
                continue;

            characterNameInputField = transform.GetComponent<InputField>();
            if (characterNameInputField == null)
                characterNameInputField = transform.GetComponentInChildren<InputField>(true);
            if (characterNameInputField == null && transform.parent != null)
                characterNameInputField = transform.parent.GetComponent<InputField>();

            if (characterNameInputField != null)
                return true;

            characterNameTmpInputField = transform.GetComponent<TMP_InputField>();
            if (characterNameTmpInputField == null)
                characterNameTmpInputField = transform.GetComponentInChildren<TMP_InputField>(true);
            if (characterNameTmpInputField == null && transform.parent != null)
                characterNameTmpInputField = transform.parent.GetComponent<TMP_InputField>();

            if (characterNameTmpInputField != null)
                return true;
        }

        return false;
    }

    private bool IsExactCharacterNameField(Transform transform)
    {
        string path = GetPlainControlPath(transform);
        return path.EndsWith(CharacterNameFieldPath, StringComparison.Ordinal) ||
               path.EndsWith(LegacyCharacterNameFieldPath, StringComparison.Ordinal);
    }

    private bool IsCharacterNameField(Transform transform)
    {
        string path = GetPlainControlPath(transform);
        return path.Contains(CharacterNameFieldPath) ||
               path.Contains(LegacyCharacterNameFieldPath);
    }

    private void LoadCharacterNameToUi()
    {
        if (characterNameInputField == null && characterNameTmpInputField == null)
            return;

        string savedName = CleanCharacterName(currentCharacter.characterName);
        if (string.IsNullOrEmpty(savedName))
            return;

        if (characterNameInputField != null)
            characterNameInputField.SetTextWithoutNotify(savedName);
        else if (characterNameTmpInputField != null)
            characterNameTmpInputField.SetTextWithoutNotify(savedName);
    }

    private void SaveCharacterNameIfPossible()
    {
        if (characterNameInputField == null && characterNameTmpInputField == null)
            return;

        string newName = null;
        if (characterNameInputField != null)
            newName = characterNameInputField.text;
        else if (characterNameTmpInputField != null)
            newName = characterNameTmpInputField.text;

        newName = CleanCharacterName(newName);
        if (!string.IsNullOrEmpty(newName))
        {
            currentCharacter.characterName = newName;
        }
    }

    private string CleanCharacterName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        value = value.Trim();
        if (float.TryParse(value, out _))
            return "";

        return value;
    }

    private string GetControlPath(Transform transform)
    {
        string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name + "/" + path;
        }

        return path;
    }

    private void SaveSharedCharacterInputs()
    {
        if (currentCharacter == null)
            return;

        foreach (InputField input in inputFields)
            SaveSharedCharacterInput(input != null ? input.transform : null, input != null ? input.text : "");

        foreach (TMP_InputField input in tmpInputFields)
            SaveSharedCharacterInput(input != null ? input.transform : null, input != null ? input.text : "");
    }

    private void SaveSharedCharacterInput(Transform transform, string value)
    {
        string key = GetSharedCharacterInputKey(transform);
        if (!string.IsNullOrEmpty(key))
            currentCharacter.SetSharedString(key, value);
    }

    private void SaveRestResourceMarkers()
    {
        if (currentSceneData == null)
            return;

        SaveMarkerParent("Rage");
        SaveMarkerParent("WildShape");
        SaveMarkerParent("ChannelDivinity");
        SaveMarkerParent("KiPoints");
        SaveMarkerParent("SorceryPoints");
        SaveMarkerParent("BloodCurse");
        SaveMarkerParent("DragonBreath");
        SaveMarkerParent("Flight");
        SaveNamedPanel("SpellSlots", "spelChek");
        SaveNamedPanel("Exhaustion", "vtoma");
        SaveNamedPanel("DeathSaves", "deadChekBox");
        SaveNamedPanel("DeathSaves", "deadCheckBox");
    }

    private void SaveMarkerParent(string markerName)
    {
        Transform marker = FindTransformByBaseName(markerName);
        if (marker != null && marker.parent != null)
            currentSceneData.SetString(RestResourceKeyPrefix + markerName, GetControlPath(marker.parent));
    }

    private void SaveNamedPanel(string keyName, string objectName)
    {
        Transform panel = FindTransformByBaseName(objectName);
        if (panel != null)
            currentSceneData.SetString(RestResourceKeyPrefix + keyName, GetControlPath(panel));
    }

    private Transform FindTransformByBaseName(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
            if (item != null && GetBaseName(item.name).Equals(objectName, StringComparison.OrdinalIgnoreCase))
                return item;

        return null;
    }

    private void LoadSharedCharacterInputs()
    {
        if (currentCharacter == null)
            return;

        foreach (InputField input in inputFields)
            LoadSharedCharacterInput(input);

        foreach (TMP_InputField input in tmpInputFields)
            LoadSharedCharacterInput(input);
    }

    private void LoadSharedCharacterInput(InputField input)
    {
        string key = GetSharedCharacterInputKey(input != null ? input.transform : null);
        if (string.IsNullOrEmpty(key))
            return;

        if (!currentCharacter.HasSharedString(key))
        {
            currentCharacter.SetSharedString(key, input != null ? input.text : "");
            return;
        }

        input.SetTextWithoutNotify(currentCharacter.GetSharedString(key, ""));
    }

    private void LoadSharedCharacterInput(TMP_InputField input)
    {
        string key = GetSharedCharacterInputKey(input != null ? input.transform : null);
        if (string.IsNullOrEmpty(key))
            return;

        if (!currentCharacter.HasSharedString(key))
        {
            currentCharacter.SetSharedString(key, input != null ? input.text : "");
            return;
        }

        input.SetTextWithoutNotify(currentCharacter.GetSharedString(key, ""));
    }

    private void ClearSharedCharacterInputs()
    {
        if (currentCharacter == null)
            return;

        if (!SceneContainsSharedCharacterInput())
            return;

        currentCharacter.DeleteSharedString("SharedInput_magMod");
        currentCharacter.DeleteSharedString("SharedInput_slogSpas");
    }

    private bool SceneContainsSharedCharacterInput()
    {
        foreach (InputField input in inputFields)
            if (!string.IsNullOrEmpty(GetSharedCharacterInputKey(input != null ? input.transform : null)))
                return true;

        foreach (TMP_InputField input in tmpInputFields)
            if (!string.IsNullOrEmpty(GetSharedCharacterInputKey(input != null ? input.transform : null)))
                return true;

        return false;
    }

    private string GetSharedCharacterInputKey(Transform transform)
    {
        string containerName = GetMatchingAncestorName(transform, "magMod", "slogSpas");
        return string.IsNullOrEmpty(containerName) ? "" : "SharedInput_" + containerName;
    }

    private string GetMatchingAncestorName(Transform transform, params string[] names)
    {
        while (transform != null)
        {
            foreach (string name in names)
                if (NameMatches(transform.name, name))
                    return name;

            transform = transform.parent;
        }

        return "";
    }

    private string GetPlainControlPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private bool NameMatches(string actualName, string expectedName)
    {
        return GetBaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private string GetBaseName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }

    #endregion

    #region UI EVENTS

    private void SubscribeToUIEvents()
    {
        foreach (var input in inputFields)
            if (input != null)
            {
                input.onEndEdit.AddListener(delegate { SaveCharacterData(); });
                input.onValueChanged.AddListener(delegate { SaveCharacterData(); });
            }

        foreach (var input in tmpInputFields)
            if (input != null)
            {
                input.onEndEdit.AddListener(delegate { SaveCharacterData(); });
                input.onValueChanged.AddListener(delegate { SaveCharacterData(); });
            }

        foreach (var toggle in toggles)
            if (toggle != null)
                toggle.onValueChanged.AddListener(delegate { SaveCharacterData(); });

        foreach (var slider in sliders)
            if (slider != null)
                slider.onValueChanged.AddListener(delegate { SaveCharacterData(); });

        foreach (var dropdown in dropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { SaveCharacterData(); });

        foreach (var dropdown in tmpDropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { SaveCharacterData(); });

        SubscribeToCharacterNameField();
    }

    private void BindResetButtons()
    {
        if (resetButton != null && !resetButtons.Contains(resetButton))
            resetButtons.Add(resetButton);

        foreach (Button button in resetButtons)
            if (button != null)
            {
                DisablePersistentOnClick(button);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ResetSceneData);
            }
    }

    public void ResetSceneData()
    {
        if (DndSaveManager.Instance == null)
            return;

        if (currentSceneData == null)
            currentSceneData = DndSaveManager.Instance.GetSceneDataForCharacter(characterId, sceneName);

        if (currentSceneData == null)
            return;

        isLoadingSceneData = true;
        try
        {
            foreach (InputField input in inputFields)
                if (input != null)
                    input.SetTextWithoutNotify("");

            foreach (TMP_InputField input in tmpInputFields)
                if (input != null)
                    input.SetTextWithoutNotify("");

            foreach (Toggle toggle in toggles)
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(false);

            foreach (Slider slider in sliders)
                if (slider != null)
                    slider.SetValueWithoutNotify(0f);

            foreach (Dropdown dropdown in dropdowns)
                if (dropdown != null)
                {
                    dropdown.SetValueWithoutNotify(0);
                    dropdown.RefreshShownValue();
                }

            foreach (TMP_Dropdown dropdown in tmpDropdowns)
                if (dropdown != null)
                {
                    dropdown.SetValueWithoutNotify(0);
                    dropdown.RefreshShownValue();
                }
        }
        finally
        {
            isLoadingSceneData = false;
        }

        DndSaveManager.Instance.ClearSceneDataFamilyForCharacter(characterId, sceneName);
        currentSceneData.ClearValues();
        ClearSharedCharacterInputs();
        CharacterPortraitManager.ClearPortraitForActiveCharacter();
        ResetInventoryCells();
        ResetSceneHealthBars();
        SaveCharacterData();
        RefreshDropdownDrivenUi();
        RefreshToggleDrivenPanels();
    }

    private void ResetInventoryCells()
    {
        InventoryItemCell[] inventoryCells = FindObjectsByType<InventoryItemCell>(FindObjectsInactive.Include);
        foreach (InventoryItemCell inventoryCell in inventoryCells)
            if (inventoryCell != null)
                inventoryCell.ResetToDefaults(false);
    }

    private void ResetSceneHealthBars()
    {
        HealthBar[] healthBars = FindObjectsByType<HealthBar>(FindObjectsInactive.Include);
        foreach (HealthBar healthBar in healthBars)
            if (healthBar != null)
                healthBar.ResetHealth();

        HealthBar1[] healthBarOnes = FindObjectsByType<HealthBar1>(FindObjectsInactive.Include);
        foreach (HealthBar1 healthBar in healthBarOnes)
            if (healthBar != null)
                healthBar.ResetHealth();
    }

    private void EnsureCharacterPortraitManager()
    {
        if (!SceneHasObject("Buttonphotopersoj") && !SceneHasObject("photopersonaja"))
            return;

        if (FindAnyObjectByType<CharacterPortraitManager>() != null)
            return;

        gameObject.AddComponent<CharacterPortraitManager>();
    }

    private bool SceneHasObject(string objectName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform transform in transforms)
            if (transform != null && NameMatches(transform.name, objectName))
                return true;

        return false;
    }

    private bool IsResetButton(Button button)
    {
        if (button == null)
            return false;

        string name = button.gameObject.name.ToLowerInvariant();
        return name.Contains("resetseve") ||
               name.Contains("reset save") ||
               name.Contains("resetsave") ||
               name.Contains("clear save");
    }

    private void DisablePersistentOnClick(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private void SubscribeToCharacterNameField()
    {
        if (characterNameInputField != null)
        {
            characterNameInputField.onEndEdit.AddListener(delegate { SaveCharacterData(); });
            characterNameInputField.onValueChanged.AddListener(delegate { SaveCharacterData(); });
        }

        if (characterNameTmpInputField != null)
        {
            characterNameTmpInputField.onEndEdit.AddListener(delegate { SaveCharacterData(); });
            characterNameTmpInputField.onValueChanged.AddListener(delegate { SaveCharacterData(); });
        }
    }

    #endregion

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveCharacterData();
    }

    private void OnApplicationQuit() => SaveCharacterData();
}
