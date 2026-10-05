using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class DndSaveManager : MonoBehaviour
{
    public static DndSaveManager Instance { get; private set; }

    public AppSaveData saveData;
    private string pendingSceneDataName;
    private string currentSceneDataName;
    private float lastCreateCharacterTime = -AppConfig.SaveData.CharacterCreateDebounceSeconds;

    private string FilePath => Path.Combine(Application.persistentDataPath, "DndCharactersData.json");
    private string BackupFilePath => FilePath + ".bak";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RuntimeLocalization.EnsureExists();
        LoadData();
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static DndSaveManager EnsureExists()
    {
        if (Instance != null)
            return Instance;

        GameObject managerObject = new GameObject("DndSaveManager");
        return managerObject.AddComponent<DndSaveManager>();
    }

    public void LoadData()
    {
        LegacyJsonLoadResult result = LegacyJsonSaveRepository.Load(FilePath, BackupFilePath);
        if (result.Source != LegacyJsonSaveSource.None)
            saveData = result.Data;

        if (result.PrimaryError != null)
            Debug.LogError("Could not load DnD save file: " + result.PrimaryError.Message);

        if (result.BackupError != null)
            Debug.LogError("Could not load DnD backup save file: " + result.BackupError.Message);

        NormalizeSaveData();
    }

    public void SaveData()
    {
        NormalizeSaveData();

        try
        {
            LegacyJsonSaveRepository.Save(FilePath, BackupFilePath, saveData);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save DnD data: " + exception.Message);
        }
    }

    public CharacterData CreateNewCharacter()
    {
        if (Time.unscaledTime - lastCreateCharacterTime <
            AppConfig.SaveData.CharacterCreateDebounceSeconds)
        {
            CharacterData activeCharacter = GetActiveCharacter();
            if (activeCharacter != null)
                return activeCharacter;
        }

        lastCreateCharacterTime = Time.unscaledTime;

        string newId = Guid.NewGuid().ToString();
        CharacterData newChar = new CharacterData(newId);
        newChar.characterName = "Новий персонаж " + (saveData.characters.Count + 1);
        newChar.maxHealth = 0;
        newChar.currentHealth = 0;
        saveData.characters.Add(newChar);
        saveData.lastActiveCharacterId = newId;
        SaveData();
        return newChar;
    }

    public CharacterData GetCharacter(string id)
    {
        return saveData.characters.Find(c => c.id == id);
    }

    public bool SetActiveCharacter(string id)
    {
        CharacterData character = GetCharacter(id);
        if (character == null)
        {
            Debug.LogError("Cannot set active character. Character not found: " + id);
            return false;
        }

        saveData.lastActiveCharacterId = id;
        SaveData();
        Debug.Log("Active DnD character: " + character.characterName + " (" + id + ")");
        return true;
    }
    
    public void DeleteCharacter(string id)
    {
        var charToDelete = GetCharacter(id);
        if (charToDelete != null)
        {
            saveData.characters.Remove(charToDelete);
            if (saveData.lastActiveCharacterId == id)
            {
                saveData.lastActiveCharacterId = saveData.characters.Count > 0 ? saveData.characters[0].id : "";
            }

            SaveData();
        }
    }

    public CharacterData GetActiveCharacter()
    {
        if (saveData == null)
        {
            LoadData();
        }

        return GetCharacter(saveData.lastActiveCharacterId);
    }

    public CharacterData EnsureActiveCharacter()
    {
        CharacterData activeCharacter = GetActiveCharacter();
        if (activeCharacter != null)
            return activeCharacter;

        if (saveData.characters.Count > 0)
        {
            activeCharacter = saveData.characters[0];
            saveData.lastActiveCharacterId = activeCharacter.id;
            SaveData();
            return activeCharacter;
        }

        return CreateNewCharacter();
    }

    public CharacterSceneData GetActiveSceneData(bool createIfMissing = true)
    {
        CharacterData activeCharacter = EnsureActiveCharacter();
        return activeCharacter.GetSceneData(GetActiveSceneDataName(), createIfMissing);
    }

    public CharacterSceneData GetSceneDataForCharacter(string characterId, string sceneName, bool createIfMissing = true)
    {
        CharacterData character = GetCharacter(characterId);
        if (character == null)
            return null;

        return character.GetSceneData(sceneName, createIfMissing);
    }

    public void ClearSceneDataFamilyForCharacter(string characterId, string sceneName)
    {
        CharacterData character = GetCharacter(characterId);
        if (character == null)
            return;

        string familyName = GetSceneDataFamilyName(sceneName);
        foreach (CharacterSceneData state in character.sceneStates)
            if (state != null && GetSceneDataFamilyName(state.sceneName) == familyName)
                state.ClearValues();
    }

    private string GetSceneDataFamilyName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "";

        sceneName = sceneName.Trim();
        int lastSpace = sceneName.LastIndexOf(' ');
        if (lastSpace > 0 && int.TryParse(sceneName.Substring(lastSpace + 1), out _))
            return sceneName.Substring(0, lastSpace);

        return sceneName;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentSceneDataName = !string.IsNullOrWhiteSpace(pendingSceneDataName) ? pendingSceneDataName : scene.name;
        pendingSceneDataName = "";

        if (!IsCharacterSheetScene(scene.name))
            return;

        if (FindAnyObjectByType<CharacterSheetManagerScene1>() != null)
            return;

        CharacterSceneAutoSave autoSave = FindAnyObjectByType<CharacterSceneAutoSave>();
        if (autoSave == null)
        {
            GameObject autoSaveObject = new GameObject("CharacterSceneAutoSave");
            autoSaveObject.AddComponent<CharacterSceneAutoSave>();
        }
    }

    private bool IsCharacterSheetScene(string sceneName)
    {
        return sceneName.Contains("cartaPersonaj") ||
               sceneName.Contains("inventory") ||
               sceneName.Contains("informForPerson") ||
               sceneName.Contains("Spels") ||
               sceneName.Contains("spelBook") ||
               sceneName.Contains("petsesn");
    }

    public void SetPendingSceneDataName(string sceneName)
    {
        pendingSceneDataName = sceneName;
    }

    public void SetActiveSceneDataName(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            currentSceneDataName = sceneName;
    }

    public string GetActiveSceneDataName()
    {
        if (!string.IsNullOrWhiteSpace(currentSceneDataName))
            return currentSceneDataName;

        return SceneManager.GetActiveScene().name;
    }

    public void NormalizeSaveData()
    {
        saveData = SaveDataNormalizer.Normalize(saveData);
    }
}

public class CharacterSceneAutoSave : MonoBehaviour
{
    private const string DropdownKeyPrefix = "Dropdown_";
    private const string TmpDropdownKeyPrefix = "TMPDropdown_";
    private const string ToggleKeyPrefix = "Toggle_";
    private const string RestResourceKeyPrefix = "RestResource_";
    private const string CharacterNameObjectName = "personajName";

    private List<InputField> inputFields = new List<InputField>();
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private List<Toggle> toggles = new List<Toggle>();
    private List<Slider> sliders = new List<Slider>();
    private List<Dropdown> dropdowns = new List<Dropdown>();
    private List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
    private List<Button> resetButtons = new List<Button>();
    private CharacterSceneData sceneData;
    private string characterId;
    private string sceneName;
    private bool isLoadingSceneData;
    private InputField characterNameInputField;
    private TMP_InputField characterNameTmpInputField;

    private void Start()
    {
        DndSaveManager.EnsureExists();
        CharacterData character = DndSaveManager.Instance.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = DndSaveManager.Instance.GetActiveSceneDataName();
        sceneData = character.GetSceneData(sceneName);
        CacheSceneControls();
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        EnsureCharacterPortraitManager();
        LoadSceneDataToUi();
        DeathSaveToggleSequence.ConfigureScene();
        RuntimeLocalization.EnsureExists().ApplyToScene();
        Subscribe();
    }

    public void SaveSceneData()
    {
        if (isLoadingSceneData)
            return;

        if (DndSaveManager.Instance == null)
            return;

        if (sceneData == null)
            sceneData = DndSaveManager.Instance.GetSceneDataForCharacter(characterId, sceneName);

        if (sceneData == null)
            return;

        sceneData.inputData.Clear();
        foreach (InputField inputField in inputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        foreach (TMP_InputField inputField in tmpInputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        SaveCharacterNameIfPossible();
        SaveSharedCharacterInputs();

        sceneData.toggleData.Clear();
        foreach (Toggle toggle in toggles)
        {
            bool isOn = toggle != null && toggle.isOn;
            sceneData.toggleData.Add(isOn);
            if (toggle != null)
                sceneData.SetInt(ToggleKeyPrefix + GetControlPath(toggle.transform), isOn ? 1 : 0);
        }

        sceneData.sliderData.Clear();
        foreach (Slider slider in sliders)
            sceneData.sliderData.Add(slider != null ? slider.value : 0f);

        sceneData.dropdownData.Clear();
        foreach (Dropdown dropdown in dropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(DropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        foreach (TMP_Dropdown dropdown in tmpDropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(TmpDropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        StableFieldStorage.Save(sceneData, inputFields);
        StableFieldStorage.Save(sceneData, tmpInputFields);
        StableFieldStorage.Save(sceneData, toggles);
        StableFieldStorage.Save(sceneData, sliders);
        StableFieldStorage.Save(sceneData, dropdowns);
        StableFieldStorage.Save(sceneData, tmpDropdowns);

        SaveRestResourceMarkers();

        DndSaveManager.Instance.SaveData();
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

        inputFields.RemoveAll(inputField => IsManagedByHealthBar(inputField != null ? inputField.transform : null));
        tmpInputFields.RemoveAll(inputField => IsManagedByHealthBar(inputField != null ? inputField.transform : null));
        toggles.RemoveAll(toggle => IsDropdownTemplatePart(toggle != null ? toggle.transform : null));
        sliders.RemoveAll(slider => IsManagedByHealthBar(slider != null ? slider.transform : null));

        inputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpInputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        toggles.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        sliders.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        dropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpDropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        resetButtons.RemoveAll(button => !IsResetButton(button));
        CacheCharacterNameField();
    }

    private bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null &&
               (transform.GetComponentInParent<HealthBar>(true) != null ||
                transform.GetComponentInParent<HealthBar1>(true) != null);
    }

    private bool IsDropdownTemplatePart(Transform transform)
    {
        while (transform != null)
        {
            if (transform.GetComponent<Dropdown>() != null || transform.GetComponent<TMP_Dropdown>() != null)
                return true;

            transform = transform.parent;
        }

        return false;
    }

    private void CacheCharacterNameField()
    {
        characterNameInputField = null;
        characterNameTmpInputField = null;

        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform item in transforms)
        {
            if (!NameMatches(item.name, CharacterNameObjectName))
                continue;

            characterNameInputField = item.GetComponent<InputField>();
            if (characterNameInputField == null)
                characterNameInputField = item.GetComponentInChildren<InputField>(true);
            if (characterNameInputField == null && item.parent != null)
                characterNameInputField = item.parent.GetComponent<InputField>();
            if (characterNameInputField != null)
                return;

            characterNameTmpInputField = item.GetComponent<TMP_InputField>();
            if (characterNameTmpInputField == null)
                characterNameTmpInputField = item.GetComponentInChildren<TMP_InputField>(true);
            if (characterNameTmpInputField == null && item.parent != null)
                characterNameTmpInputField = item.parent.GetComponent<TMP_InputField>();
            if (characterNameTmpInputField != null)
                return;
        }
    }

    private void LoadCharacterNameToUi()
    {
        if (characterNameInputField == null && characterNameTmpInputField == null)
            return;

        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        if (character == null)
            return;

        string savedName = CleanCharacterName(character.characterName);
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

        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        if (character == null)
            return;

        string newName = null;
        if (characterNameInputField != null)
            newName = characterNameInputField.text;
        else if (characterNameTmpInputField != null)
            newName = characterNameTmpInputField.text;

        newName = CleanCharacterName(newName);
        if (!string.IsNullOrEmpty(newName))
            character.characterName = newName;
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

    private void LoadSceneDataToUi()
    {
        isLoadingSceneData = true;
        try
        {
            int dataIndex = 0;

            for (int i = 0; i < inputFields.Count; i++, dataIndex++)
                if (inputFields[i] != null)
                    inputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, inputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < tmpInputFields.Count; i++, dataIndex++)
                if (tmpInputFields[i] != null)
                    tmpInputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, tmpInputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < toggles.Count; i++)
                if (toggles[i] != null)
                {
                    string key = ToggleKeyPrefix + GetControlPath(toggles[i].transform);
                    bool value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key) != 0
                        : i < sceneData.toggleData.Count && sceneData.toggleData[i];
                    toggles[i].SetIsOnWithoutNotify(StableFieldStorage.ReadInt(sceneData, toggles[i], value ? 1 : 0) != 0);
                }

            for (int i = 0; i < sliders.Count; i++)
                if (sliders[i] != null)
                    sliders[i].SetValueWithoutNotify(StableFieldStorage.ReadFloat(sceneData, sliders[i], i < sceneData.sliderData.Count ? sceneData.sliderData[i] : 0f));

            for (int i = 0; i < dropdowns.Count; i++)
                if (dropdowns[i] != null)
                {
                    string key = DropdownKeyPrefix + GetControlPath(dropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i < sceneData.dropdownData.Count ? sceneData.dropdownData[i] : 0;
                    dropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, dropdowns[i], value));
                    dropdowns[i].RefreshShownValue();
                }

            int tmpDropdownOffset = dropdowns.Count;
            for (int i = 0; i < tmpDropdowns.Count; i++)
                if (tmpDropdowns[i] != null)
                {
                    string key = TmpDropdownKeyPrefix + GetControlPath(tmpDropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i + tmpDropdownOffset < sceneData.dropdownData.Count ? sceneData.dropdownData[i + tmpDropdownOffset] : 0;
                    tmpDropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, tmpDropdowns[i], value));
                    tmpDropdowns[i].RefreshShownValue();
                }

            LoadSharedCharacterInputs();
            LoadCharacterNameToUi();
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

        SaveSceneData();

        CharacterData character = DndSaveManager.Instance.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = newSceneName;
        DndSaveManager.Instance.SetActiveSceneDataName(sceneName);
        sceneData = character.GetSceneData(sceneName);

        LoadSceneDataToUi();
        RuntimeLocalization.EnsureExists().ApplyToScene();
    }

    private void Subscribe()
    {
        foreach (InputField inputField in inputFields)
            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(delegate { SaveSceneData(); });
                inputField.onValueChanged.AddListener(delegate { SaveSceneData(); });
            }

        foreach (TMP_InputField inputField in tmpInputFields)
            if (inputField != null)
            {
                inputField.onEndEdit.AddListener(delegate { SaveSceneData(); });
                inputField.onValueChanged.AddListener(delegate { SaveSceneData(); });
            }

        foreach (Toggle toggle in toggles)
            if (toggle != null)
                toggle.onValueChanged.AddListener(delegate { SaveSceneData(); });

        foreach (Slider slider in sliders)
            if (slider != null)
                slider.onValueChanged.AddListener(delegate { SaveSceneData(); });

        foreach (Dropdown dropdown in dropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { SaveSceneData(); });

        foreach (TMP_Dropdown dropdown in tmpDropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { SaveSceneData(); });

        foreach (Button resetButton in resetButtons)
            if (resetButton != null)
            {
                DisablePersistentOnClick(resetButton);
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetSceneData);
            }
    }

    public void ResetSceneData()
    {
        if (DndSaveManager.Instance == null)
            return;

        if (sceneData == null)
            sceneData = DndSaveManager.Instance.GetSceneDataForCharacter(characterId, sceneName);

        if (sceneData == null)
            return;

        isLoadingSceneData = true;
        try
        {
            foreach (InputField inputField in inputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

            foreach (TMP_InputField inputField in tmpInputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

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
        sceneData.ClearValues();
        ClearSharedCharacterInputs();
        CharacterPortraitManager.ClearPortraitForActiveCharacter();
        ResetInventoryCells();
        ResetSceneHealthBars();
        SaveSceneData();
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
            if (transform != null && GetBaseName(transform.name).Equals(objectName, StringComparison.OrdinalIgnoreCase))
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
        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        if (character == null)
            return;

        foreach (InputField input in inputFields)
            SaveSharedCharacterInput(character, input != null ? input.transform : null, input != null ? input.text : "");

        foreach (TMP_InputField input in tmpInputFields)
            SaveSharedCharacterInput(character, input != null ? input.transform : null, input != null ? input.text : "");
    }

    private void SaveSharedCharacterInput(CharacterData character, Transform transform, string value)
    {
        string key = GetSharedCharacterInputKey(transform);
        if (!string.IsNullOrEmpty(key))
            character.SetSharedString(key, value);
    }

    private void SaveRestResourceMarkers()
    {
        if (sceneData == null)
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
            sceneData.SetString(RestResourceKeyPrefix + markerName, GetControlPath(marker.parent));
    }

    private void SaveNamedPanel(string keyName, string objectName)
    {
        Transform panel = FindTransformByBaseName(objectName);
        if (panel != null)
            sceneData.SetString(RestResourceKeyPrefix + keyName, GetControlPath(panel));
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
        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        if (character == null)
            return;

        foreach (InputField input in inputFields)
            LoadSharedCharacterInput(character, input);

        foreach (TMP_InputField input in tmpInputFields)
            LoadSharedCharacterInput(character, input);
    }

    private void LoadSharedCharacterInput(CharacterData character, InputField input)
    {
        string key = GetSharedCharacterInputKey(input != null ? input.transform : null);
        if (string.IsNullOrEmpty(key))
            return;

        if (!character.HasSharedString(key))
        {
            character.SetSharedString(key, input != null ? input.text : "");
            return;
        }

        input.SetTextWithoutNotify(character.GetSharedString(key, ""));
    }

    private void LoadSharedCharacterInput(CharacterData character, TMP_InputField input)
    {
        string key = GetSharedCharacterInputKey(input != null ? input.transform : null);
        if (string.IsNullOrEmpty(key))
            return;

        if (!character.HasSharedString(key))
        {
            character.SetSharedString(key, input != null ? input.text : "");
            return;
        }

        input.SetTextWithoutNotify(character.GetSharedString(key, ""));
    }

    private void ClearSharedCharacterInputs()
    {
        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        if (character == null || !SceneContainsSharedCharacterInput())
            return;

        character.DeleteSharedString("SharedInput_magMod");
        character.DeleteSharedString("SharedInput_slogSpas");
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

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            SaveSceneData();
    }

    private void OnDisable()
    {
        SaveSceneData();
    }

    private void OnApplicationQuit()
    {
        SaveSceneData();
    }
}
