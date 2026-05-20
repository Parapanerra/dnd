using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class CharacterData
{
    public string id;
    public string characterName = "Новий персонаж";
    public int maxHealth;
    public int currentHealth;

    // Legacy fields kept so old saves do not break. New data is stored per scene in sceneStates.
    public List<string> inputData = new List<string>();
    public List<bool> toggleData = new List<bool>();
    public List<float> sliderData = new List<float>();
    public List<int> dropdownData = new List<int>();
    public List<CharacterSceneData> sceneStates = new List<CharacterSceneData>();

    public CharacterData(string newId)
    {
        id = newId;
    }

    public CharacterSceneData GetSceneData(string sceneName, bool createIfMissing = true)
    {
        CharacterSceneData state = sceneStates.Find(s => s.sceneName == sceneName);
        if (state == null && createIfMissing)
        {
            state = new CharacterSceneData(sceneName);
            sceneStates.Add(state);
        }

        return state;
    }
}

[System.Serializable]
public class CharacterSceneData
{
    public string sceneName;
    public List<string> inputData = new List<string>();
    public List<bool> toggleData = new List<bool>();
    public List<float> sliderData = new List<float>();
    public List<int> dropdownData = new List<int>();
    public List<StringSaveEntry> stringData = new List<StringSaveEntry>();
    public List<IntSaveEntry> intData = new List<IntSaveEntry>();

    public CharacterSceneData(string newSceneName)
    {
        sceneName = newSceneName;
    }

    public string GetString(string key, string defaultValue = "")
    {
        StringSaveEntry entry = stringData.Find(item => item.key == key);
        return entry != null ? entry.value : defaultValue;
    }

    public void SetString(string key, string value)
    {
        StringSaveEntry entry = stringData.Find(item => item.key == key);
        if (entry == null)
        {
            entry = new StringSaveEntry { key = key };
            stringData.Add(entry);
        }

        entry.value = value;
    }

    public void DeleteString(string key)
    {
        stringData.RemoveAll(item => item.key == key);
    }

    public bool HasString(string key)
    {
        return stringData.Exists(item => item.key == key);
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        IntSaveEntry entry = intData.Find(item => item.key == key);
        return entry != null ? entry.value : defaultValue;
    }

    public bool HasInt(string key)
    {
        return intData.Exists(item => item.key == key);
    }

    public void SetInt(string key, int value)
    {
        IntSaveEntry entry = intData.Find(item => item.key == key);
        if (entry == null)
        {
            entry = new IntSaveEntry { key = key };
            intData.Add(entry);
        }

        entry.value = value;
    }

    public void ClearValues()
    {
        inputData.Clear();
        toggleData.Clear();
        sliderData.Clear();
        dropdownData.Clear();
        stringData.Clear();
        intData.Clear();
    }
}

[System.Serializable]
public class StringSaveEntry
{
    public string key;
    public string value;
}

[System.Serializable]
public class IntSaveEntry
{
    public string key;
    public int value;
}

[System.Serializable]
public class AppSaveData
{
    public string lastActiveCharacterId;
    public List<CharacterData> characters = new List<CharacterData>();
}

public class DndSaveManager : MonoBehaviour
{
    public static DndSaveManager Instance { get; private set; }

    public AppSaveData saveData;
    private string pendingSceneDataName;
    private string currentSceneDataName;
    private float lastCreateCharacterTime = -10f;

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
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                saveData = JsonUtility.FromJson<AppSaveData>(json);
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not load DnD save file: " + exception.Message);
            TryLoadBackup();
        }

        if (saveData == null)
        {
            saveData = new AppSaveData();
        }

        if (saveData.characters == null)
        {
            saveData.characters = new List<CharacterData>();
        }

        NormalizeSaveData();
    }

    public void SaveData()
    {
        NormalizeSaveData();

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            string json = JsonUtility.ToJson(saveData, true);
            string tempFilePath = FilePath + ".tmp";
            File.WriteAllText(tempFilePath, json);

            if (File.Exists(FilePath))
            {
                File.Copy(FilePath, BackupFilePath, true);
                File.Delete(FilePath);
            }

            File.Move(tempFilePath, FilePath);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save DnD data: " + exception.Message);
        }
    }

    public CharacterData CreateNewCharacter()
    {
        if (Time.unscaledTime - lastCreateCharacterTime < 0.5f)
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

        if (FindFirstObjectByType<CharacterSheetManagerScene1>() != null)
            return;

        CharacterSceneAutoSave autoSave = FindFirstObjectByType<CharacterSceneAutoSave>();
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

    private void TryLoadBackup()
    {
        try
        {
            if (!File.Exists(BackupFilePath))
                return;

            string json = File.ReadAllText(BackupFilePath);
            saveData = JsonUtility.FromJson<AppSaveData>(json);
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not load DnD backup save file: " + exception.Message);
        }
    }

    public void NormalizeSaveData()
    {
        if (saveData == null)
        {
            saveData = new AppSaveData();
        }

        if (saveData.characters == null)
        {
            saveData.characters = new List<CharacterData>();
        }

        foreach (CharacterData character in saveData.characters)
        {
            if (string.IsNullOrEmpty(character.id))
                character.id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(character.characterName) || IsNumericName(character.characterName))
                character.characterName = "Новий персонаж";

            if (character.inputData == null)
                character.inputData = new List<string>();

            if (character.toggleData == null)
                character.toggleData = new List<bool>();

            if (character.sliderData == null)
                character.sliderData = new List<float>();

            if (character.dropdownData == null)
                character.dropdownData = new List<int>();

            if (character.sceneStates == null)
                character.sceneStates = new List<CharacterSceneData>();

            foreach (CharacterSceneData sceneData in character.sceneStates)
            {
                if (sceneData.inputData == null)
                    sceneData.inputData = new List<string>();

                if (sceneData.toggleData == null)
                    sceneData.toggleData = new List<bool>();

                if (sceneData.sliderData == null)
                    sceneData.sliderData = new List<float>();

                if (sceneData.dropdownData == null)
                    sceneData.dropdownData = new List<int>();

                if (sceneData.stringData == null)
                    sceneData.stringData = new List<StringSaveEntry>();

                if (sceneData.intData == null)
                    sceneData.intData = new List<IntSaveEntry>();
            }
        }
    }

    private bool IsNumericName(string value)
    {
        return float.TryParse(value, out _);
    }
}

public class CharacterSceneAutoSave : MonoBehaviour
{
    private const string DropdownKeyPrefix = "Dropdown_";
    private const string TmpDropdownKeyPrefix = "TMPDropdown_";

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

    private void Start()
    {
        DndSaveManager.EnsureExists();
        CharacterData character = DndSaveManager.Instance.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = DndSaveManager.Instance.GetActiveSceneDataName();
        sceneData = character.GetSceneData(sceneName);
        CacheSceneControls();
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        LoadSceneDataToUi();
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

        sceneData.toggleData.Clear();
        foreach (Toggle toggle in toggles)
            sceneData.toggleData.Add(toggle != null && toggle.isOn);

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

        DndSaveManager.Instance.SaveData();
    }

    private void CacheSceneControls()
    {
        inputFields = new List<InputField>(FindObjectsByType<InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        tmpInputFields = new List<TMP_InputField>(FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        toggles = new List<Toggle>(FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        sliders = new List<Slider>(FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        dropdowns = new List<Dropdown>(FindObjectsByType<Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        tmpDropdowns = new List<TMP_Dropdown>(FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        resetButtons = new List<Button>(FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None));

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

    private void LoadSceneDataToUi()
    {
        isLoadingSceneData = true;
        try
        {
            int dataIndex = 0;

            for (int i = 0; i < inputFields.Count; i++, dataIndex++)
                if (inputFields[i] != null)
                    inputFields[i].SetTextWithoutNotify(dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : "");

            for (int i = 0; i < tmpInputFields.Count; i++, dataIndex++)
                if (tmpInputFields[i] != null)
                    tmpInputFields[i].SetTextWithoutNotify(dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : "");

            for (int i = 0; i < toggles.Count; i++)
                if (toggles[i] != null)
                    toggles[i].SetIsOnWithoutNotify(i < sceneData.toggleData.Count && sceneData.toggleData[i]);

            for (int i = 0; i < sliders.Count; i++)
                if (sliders[i] != null)
                    sliders[i].SetValueWithoutNotify(i < sceneData.sliderData.Count ? sceneData.sliderData[i] : 0f);

            for (int i = 0; i < dropdowns.Count; i++)
                if (dropdowns[i] != null)
                {
                    string key = DropdownKeyPrefix + GetControlPath(dropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i < sceneData.dropdownData.Count ? sceneData.dropdownData[i] : 0;
                    dropdowns[i].SetValueWithoutNotify(value);
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
                    tmpDropdowns[i].SetValueWithoutNotify(value);
                    tmpDropdowns[i].RefreshShownValue();
                }
        }
        finally
        {
            isLoadingSceneData = false;
        }

        RefreshDropdownDrivenUi();
    }

    private void RefreshDropdownDrivenUi()
    {
        DropdownManager[] dropdownManagers = FindObjectsByType<DropdownManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DropdownManager manager in dropdownManagers)
            if (manager != null)
                manager.RefreshAll();

        DropdownVisibilityController[] visibilityControllers = FindObjectsByType<DropdownVisibilityController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (DropdownVisibilityController controller in visibilityControllers)
            if (controller != null)
                controller.RefreshVisibility();
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
        SaveSceneData();
        RefreshDropdownDrivenUi();
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
