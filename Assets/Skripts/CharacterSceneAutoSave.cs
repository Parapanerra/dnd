using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CharacterSceneAutoSave : MonoBehaviour
{
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
    private readonly SceneSaveController sceneFields = new SceneSaveController();
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

    private void SaveIdentityAndSharedInputs()
    {
        SaveCharacterNameIfPossible();
        SaveSharedCharacterInputs();
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

        sceneFields.Save(sceneData, SaveIdentityAndSharedInputs);

        SaveRestResourceMarkers();

        DndSaveManager.Instance.RequestSaveData();
    }

    private void CacheSceneControls()
    {
        sceneFields.Collect(IsManagedByHealthBar, excludeDropdownTemplates: true);
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
            sceneFields.Load(sceneData);

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
        DndSaveManager.Instance.FlushPendingSave();

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
        sceneFields.Subscribe(SaveSceneData, () => DndSaveManager.Instance?.FlushPendingSave());

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
            sceneFields.Reset();
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
        {
            SaveSceneData();
            DndSaveManager.Instance?.FlushPendingSave();
        }
    }

    private void OnDisable()
    {
        SaveSceneData();
        DndSaveManager.Instance?.FlushPendingSave();
    }

    private void OnApplicationQuit()
    {
        SaveSceneData();
        DndSaveManager.Instance?.FlushPendingSave();
    }
}
