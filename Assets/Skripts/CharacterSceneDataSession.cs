using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Owns the active character scene's in-memory state, field bindings, and save lifecycle.
internal sealed class CharacterSceneDataSession
{
    private readonly DndSaveManager saveManager;
    private readonly bool isMainSheetScene;
    private readonly SceneSaveController sceneFields = new SceneSaveController();
    private readonly CharacterNameFieldService characterNameField = new CharacterNameFieldService();
    private readonly SharedCharacterInputService sharedInputs = new SharedCharacterInputService();
    private readonly CharacterSceneMigrationService sceneMigration = new CharacterSceneMigrationService();
    private readonly SceneExitSaveGate exitSave = new SceneExitSaveGate();
    private CharacterSceneSaveService sceneSaveService;
    private CharacterData currentCharacter;
    private CharacterSceneData currentSceneData;
    private List<InputField> inputFields;
    private List<TMP_InputField> tmpInputFields;
    private bool isLoadingSceneData;
    private string characterId;
    private string sceneName;

    internal IReadOnlyList<Button> ResetButtons => sceneFields.ResetButtons;
    internal string CharacterId => characterId;
    internal bool IsReady => sceneSaveService != null;

    internal CharacterSceneDataSession(DndSaveManager saveManager, bool isMainSheetScene)
    {
        this.saveManager = saveManager;
        this.isMainSheetScene = isMainSheetScene;
    }

    internal bool Initialize()
    {
        currentCharacter = saveManager.EnsureActiveCharacter();
        if (currentCharacter == null)
            return false;

        characterId = currentCharacter.id;
        sceneName = saveManager.GetActiveSceneDataName();
        currentSceneData = currentCharacter.GetSceneData(sceneName);
        sceneFields.Collect(IsManagedByHealthBar, excludeDropdownTemplates: !isMainSheetScene);
        inputFields = new List<InputField>(sceneFields.InputFields);
        tmpInputFields = new List<TMP_InputField>(sceneFields.TmpInputFields);
        characterNameField.Cache(inputFields, tmpInputFields, isMainSheetScene);
        sceneSaveService = new CharacterSceneSaveService(saveManager, sceneFields, SaveIdentityAndSharedInputs);
        return true;
    }

    internal void Load()
    {
        if (currentCharacter == null || currentSceneData == null)
            return;

        if (isMainSheetScene)
            sceneMigration.CopyLegacyFieldsIfEmpty(currentCharacter, currentSceneData);

        isLoadingSceneData = true;
        try
        {
            sceneFields.Load(currentSceneData);
            characterNameField.Load(currentCharacter);
            sharedInputs.Load(currentCharacter, inputFields, tmpInputFields);
        }
        finally
        {
            isLoadingSceneData = false;
        }
    }

    internal void Save()
    {
        if (isLoadingSceneData || sceneSaveService == null)
            return;

        CharacterSceneData saved = sceneSaveService.Save(characterId, sceneName);
        if (saved == null)
            return;

        currentCharacter = saveManager.GetCharacter(characterId);
        currentSceneData = saved;
        exitSave.NoteChange();
    }

    internal bool Switch(string newSceneName)
    {
        if (string.IsNullOrWhiteSpace(newSceneName))
            return false;

        Save();
        saveManager.FlushPendingSave();
        currentCharacter = saveManager.GetCharacter(characterId) ?? saveManager.EnsureActiveCharacter();
        if (currentCharacter == null)
            return false;

        characterId = currentCharacter.id;
        sceneName = newSceneName;
        saveManager.SetActiveSceneDataName(sceneName);
        currentSceneData = currentCharacter.GetSceneData(sceneName);
        Load();
        return true;
    }

    internal bool ResetData()
    {
        if (currentSceneData == null)
            currentSceneData = saveManager.GetSceneDataForCharacter(characterId, sceneName);
        if (currentSceneData == null)
            return false;

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
        sharedInputs.Clear(currentCharacter, inputFields, tmpInputFields);
        return true;
    }

    internal void Subscribe()
    {
        sceneFields.Subscribe(Save, () => saveManager.FlushPendingSave());
        if (isMainSheetScene)
            characterNameField.SubscribeIfOutsideCollectedFields(inputFields, tmpInputFields,
                Save, () => saveManager.FlushPendingSave());
    }

    internal void NoteChange()
    {
        exitSave.NoteChange();
    }

    internal void FlushOnExit()
    {
        if (sceneSaveService == null)
            return;

        exitSave.Flush(() =>
        {
            Save();
            saveManager.FlushPendingSave();
            return !saveManager.HasPendingSave;
        });
    }

    private void SaveIdentityAndSharedInputs(CharacterData character)
    {
        characterNameField.Save(character);
        sharedInputs.Save(character, inputFields, tmpInputFields);
    }

    private bool IsManagedByHealthBar(Transform transform)
    {
        return transform != null && transform.GetComponentInParent<HealthBar>(true) != null;
    }
}
