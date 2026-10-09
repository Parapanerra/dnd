using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class CharacterSheetManagerScene1 : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private List<InputField> inputFields;
    [SerializeField] private List<Toggle> toggles;
    [SerializeField] private List<Slider> sliders;
    [SerializeField] private List<Dropdown> dropdowns;
    
    [Header("Buttons")]
    [SerializeField] private Button backToMenuButton;
    [SerializeField] private Button deleteCharacterButton;
    [SerializeField] private Button resetButton;

    private DndSaveManager saveManager;
    private CharacterSceneSaveService sceneSaveService;
    private CharacterData currentCharacter;
    private CharacterSceneData currentSceneData;
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private string characterId;
    private string sceneName;
    private readonly CharacterNameFieldService characterNameField = new CharacterNameFieldService();
    private bool isLoadingSceneData;
    private readonly SceneSaveController sceneFields = new SceneSaveController();
    private List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
    private List<Button> resetButtons = new List<Button>();

    private void Start()
    {
        if (!SceneManager.GetActiveScene().name.StartsWith(AppConfig.Scenes.CharacterSheet, StringComparison.Ordinal))
            return;

        saveManager = DndSaveManager.EnsureExists();

        currentCharacter = saveManager.EnsureActiveCharacter();
        characterId = currentCharacter.id;
        sceneName = saveManager.GetActiveSceneDataName();
        currentSceneData = currentCharacter.GetSceneData(sceneName);
        CacheSceneControls();
        sceneSaveService = new CharacterSceneSaveService(saveManager, sceneFields, SaveIdentityAndSharedInputs);
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        CharacterSceneUiService.EnsurePortraitManager(gameObject);

        if (currentCharacter == null)
        {
            Debug.LogError("Could not find or create active character.");
            return;
        }

        LoadCharacterDataToUI();
        DeathSaveToggleSequence.ConfigureScene(saveManager);
        RuntimeLocalization.EnsureExists().ApplyToScene();

        SubscribeToUIEvents();

        if (backToMenuButton != null)
            backToMenuButton.onClick.AddListener(() => SceneManager.LoadScene("menu"));

        if (deleteCharacterButton != null)
        {
            deleteCharacterButton.onClick.AddListener(() => 
            {
                saveManager.DeleteCharacter(characterId);
                SceneManager.LoadScene("menu");
            });
        }

        BindResetButtons();
    }

    #region ЗБЕРЕЖЕННЯ / ЗАВАНТАЖЕННЯ (ООП)

    private void SaveIdentityAndSharedInputs(CharacterData character)
    {
        characterNameField.Save(character);
        SharedCharacterInputService.Save(character, inputFields, tmpInputFields);
    }

    public void SaveCharacterData()
    {
        if (isLoadingSceneData || sceneSaveService == null)
            return;

        CharacterSceneData saved = sceneSaveService.Save(characterId, sceneName);
        if (saved == null)
            return;

        currentCharacter = saveManager.GetCharacter(characterId);
        currentSceneData = saved;
    }

    private void LoadCharacterDataToUI()
    {
        if (currentCharacter == null || currentSceneData == null) return;

        CharacterSceneMigrationService.CopyLegacyFieldsIfEmpty(currentCharacter, currentSceneData);

        isLoadingSceneData = true;
        try
        {
            sceneFields.Load(currentSceneData);

            characterNameField.Load(currentCharacter);
            LoadSharedCharacterInputs();
        }
        finally
        {
            isLoadingSceneData = false;
        }

        CharacterSceneUiService.RefreshAfterLoad();
    }

    public void SwitchSceneData(string newSceneName)
    {
        if (string.IsNullOrWhiteSpace(newSceneName) || saveManager == null)
            return;

        SaveCharacterData();
        saveManager.FlushPendingSave();

        currentCharacter = saveManager.GetCharacter(characterId);
        if (currentCharacter == null)
            currentCharacter = saveManager.EnsureActiveCharacter();

        characterId = currentCharacter.id;
        sceneName = newSceneName;
        saveManager.SetActiveSceneDataName(sceneName);
        currentSceneData = currentCharacter.GetSceneData(sceneName);

        LoadCharacterDataToUI();
        RuntimeLocalization.EnsureExists().ApplyToScene();
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
        resetButtons.RemoveAll(button => !CharacterSceneUiService.IsResetButton(button));

        characterNameField.Cache(inputFields, tmpInputFields, true);
    }

    private bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null && transform.GetComponentInParent<HealthBar>(true) != null;
    }


    private void LoadSharedCharacterInputs()
    {
        SharedCharacterInputService.Load(currentCharacter, inputFields, tmpInputFields);
    }

    private void ClearSharedCharacterInputs()
    {
        SharedCharacterInputService.Clear(currentCharacter, inputFields, tmpInputFields);
    }

    #endregion

    #region UI EVENTS

    private void SubscribeToUIEvents()
    {
        sceneFields.Subscribe(SaveCharacterData, () => saveManager?.FlushPendingSave());

        characterNameField.SubscribeIfOutsideCollectedFields(inputFields, tmpInputFields,
            SaveCharacterData, () => saveManager?.FlushPendingSave());
    }

    private void BindResetButtons()
    {
        if (resetButton != null && !resetButtons.Contains(resetButton))
            resetButtons.Add(resetButton);

        foreach (Button button in resetButtons)
            if (button != null)
            {
                CharacterSceneUiService.DisablePersistentOnClick(button);
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ResetSceneData);
            }
    }

    public void ResetSceneData()
    {
        if (saveManager == null)
            return;

        if (currentSceneData == null)
            currentSceneData = saveManager.GetSceneDataForCharacter(characterId, sceneName);

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

        saveManager.ClearSceneDataFamilyForCharacter(characterId, sceneName);
        currentSceneData.ClearValues();
        ClearSharedCharacterInputs();
        CharacterPortraitManager.ClearPortraitForActiveCharacter();
        CharacterSceneUiService.ResetSceneWidgets();
        SaveCharacterData();
        CharacterSceneUiService.RefreshAfterReset();
    }

    #endregion

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveCharacterData();
            saveManager?.FlushPendingSave();
        }
    }

    private void OnDisable()
    {
        SaveCharacterData();
        saveManager?.FlushPendingSave();
    }

    private void OnApplicationQuit()
    {
        SaveCharacterData();
        saveManager?.FlushPendingSave();
    }
}
