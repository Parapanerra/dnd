using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSceneAutoSave : MonoBehaviour
{
    private List<InputField> inputFields = new List<InputField>();
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private List<Button> resetButtons = new List<Button>();
    private DndSaveManager saveManager;
    private CharacterSceneSaveService sceneSaveService;
    private CharacterSceneData sceneData;
    private string characterId;
    private string sceneName;
    private bool isLoadingSceneData;
    private readonly SceneSaveController sceneFields = new SceneSaveController();
    private readonly CharacterNameFieldService characterNameField = new CharacterNameFieldService();

    private void Start()
    {
        saveManager = DndSaveManager.EnsureExists();
        CharacterData character = saveManager.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = saveManager.GetActiveSceneDataName();
        sceneData = character.GetSceneData(sceneName);
        CacheSceneControls();
        sceneSaveService = new CharacterSceneSaveService(saveManager, sceneFields, SaveIdentityAndSharedInputs);
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        CharacterSceneUiService.EnsurePortraitManager(gameObject);
        LoadSceneDataToUi();
        DeathSaveToggleSequence.ConfigureScene(saveManager);
        RuntimeLocalization.EnsureExists().ApplyToScene();
        Subscribe();
    }

    private void SaveIdentityAndSharedInputs(CharacterData character)
    {
        characterNameField.Save(character);
        SharedCharacterInputService.Save(character, inputFields, tmpInputFields);
    }

    public void SaveSceneData()
    {
        if (isLoadingSceneData || sceneSaveService == null)
            return;

        CharacterSceneData saved = sceneSaveService.Save(characterId, sceneName);
        if (saved != null)
            sceneData = saved;
    }

    private void CacheSceneControls()
    {
        sceneFields.Collect(IsManagedByHealthBar, excludeDropdownTemplates: true);
        inputFields = sceneFields.InputFields;
        tmpInputFields = sceneFields.TmpInputFields;
        resetButtons = sceneFields.ResetButtons;
        resetButtons.RemoveAll(button => !CharacterSceneUiService.IsResetButton(button));
        characterNameField.Cache(inputFields, tmpInputFields, false);
    }

    private bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null && transform.GetComponentInParent<HealthBar>(true) != null;
    }

    private void LoadSceneDataToUi()
    {
        isLoadingSceneData = true;
        try
        {
            sceneFields.Load(sceneData);

            LoadSharedCharacterInputs();
            characterNameField.Load(saveManager != null ? saveManager.GetCharacter(characterId) : null);
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

        SaveSceneData();
        saveManager.FlushPendingSave();

        CharacterData character = saveManager.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = newSceneName;
        saveManager.SetActiveSceneDataName(sceneName);
        sceneData = character.GetSceneData(sceneName);

        LoadSceneDataToUi();
        RuntimeLocalization.EnsureExists().ApplyToScene();
    }

    private void Subscribe()
    {
        sceneFields.Subscribe(SaveSceneData, () => saveManager?.FlushPendingSave());

        foreach (Button resetButton in resetButtons)
            if (resetButton != null)
            {
                CharacterSceneUiService.DisablePersistentOnClick(resetButton);
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetSceneData);
            }
    }

    public void ResetSceneData()
    {
        if (saveManager == null)
            return;

        if (sceneData == null)
            sceneData = saveManager.GetSceneDataForCharacter(characterId, sceneName);

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

        saveManager.ClearSceneDataFamilyForCharacter(characterId, sceneName);
        sceneData.ClearValues();
        ClearSharedCharacterInputs();
        CharacterPortraitManager.ClearPortraitForActiveCharacter();
        CharacterSceneUiService.ResetSceneWidgets();
        SaveSceneData();
        CharacterSceneUiService.RefreshAfterReset();
    }


    private void LoadSharedCharacterInputs()
    {
        CharacterData character = saveManager != null ? saveManager.GetCharacter(characterId) : null;
        SharedCharacterInputService.Load(character, inputFields, tmpInputFields);
    }

    private void ClearSharedCharacterInputs()
    {
        CharacterData character = saveManager != null ? saveManager.GetCharacter(characterId) : null;
        SharedCharacterInputService.Clear(character, inputFields, tmpInputFields);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveSceneData();
            saveManager?.FlushPendingSave();
        }
    }

    private void OnDisable()
    {
        SaveSceneData();
        saveManager?.FlushPendingSave();
    }

    private void OnApplicationQuit()
    {
        SaveSceneData();
        saveManager?.FlushPendingSave();
    }
}
