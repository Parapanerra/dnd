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
    private readonly SceneSaveController sceneFields = new SceneSaveController();
    private List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
    private List<Button> resetButtons = new List<Button>();

    private void Start()
    {
        if (!SceneManager.GetActiveScene().name.StartsWith(AppConfig.Scenes.CharacterSheet, StringComparison.Ordinal))
            return;

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

    private void SaveIdentityAndSharedInputs()
    {
        SaveCharacterNameIfPossible();
        SaveSharedCharacterInputs();
    }

    public void SaveCharacterData()
    {
        if (isLoadingSceneData || DndSaveManager.Instance == null)
            return;

        if (DndSaveManager.Instance != null)
        {
            currentCharacter = DndSaveManager.Instance.GetCharacter(characterId);
            currentSceneData = DndSaveManager.Instance.GetSceneDataForCharacter(characterId, sceneName);
        }

        if (currentCharacter == null || currentSceneData == null) return;

        sceneFields.Save(currentSceneData, SaveIdentityAndSharedInputs);

        SaveRestResourceMarkers();

        DndSaveManager.Instance.RequestSaveData();
    }

    private void LoadCharacterDataToUI()
    {
        if (currentCharacter == null || currentSceneData == null) return;

        MigrateLegacySceneDataIfNeeded();

        isLoadingSceneData = true;
        try
        {
            sceneFields.Load(currentSceneData);

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
        DndSaveManager.Instance.FlushPendingSave();

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
        sceneFields.Collect(IsManagedByHealthBar, excludeDropdownTemplates: false);
        inputFields = sceneFields.InputFields;
        tmpInputFields = sceneFields.TmpInputFields;
        toggles = sceneFields.Toggles;
        sliders = sceneFields.Sliders;
        dropdowns = sceneFields.Dropdowns;
        tmpDropdowns = sceneFields.TmpDropdowns;
        resetButtons = sceneFields.ResetButtons;
        resetButtons.RemoveAll(button => !IsResetButton(button));

        CacheCharacterNameField();
    }

    private bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null &&
               (transform.GetComponentInParent<HealthBar>(true) != null ||
                transform.GetComponentInParent<HealthBar1>(true) != null);
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
        SceneRoleLookup.SaveRestResourcePaths(currentSceneData);
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
        sceneFields.Subscribe(SaveCharacterData, () => DndSaveManager.Instance?.FlushPendingSave());

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
            sceneFields.Reset();
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

        SceneRoleMarker marker = button.GetComponent<SceneRoleMarker>();
        if (marker != null)
            return marker.role == SceneRole.ResetScene;

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
        if (characterNameInputField != null && !inputFields.Contains(characterNameInputField))
        {
            characterNameInputField.onValueChanged.AddListener(delegate { SaveCharacterData(); });
            characterNameInputField.onEndEdit.AddListener(delegate { DndSaveManager.Instance?.FlushPendingSave(); });
        }

        if (characterNameTmpInputField != null && !tmpInputFields.Contains(characterNameTmpInputField))
        {
            characterNameTmpInputField.onValueChanged.AddListener(delegate { SaveCharacterData(); });
            characterNameTmpInputField.onEndEdit.AddListener(delegate { DndSaveManager.Instance?.FlushPendingSave(); });
        }
    }

    #endregion

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveCharacterData();
            DndSaveManager.Instance?.FlushPendingSave();
        }
    }

    private void OnDisable()
    {
        SaveCharacterData();
        DndSaveManager.Instance?.FlushPendingSave();
    }

    private void OnApplicationQuit()
    {
        SaveCharacterData();
        DndSaveManager.Instance?.FlushPendingSave();
    }
}
