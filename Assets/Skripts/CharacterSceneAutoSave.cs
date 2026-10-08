using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSceneAutoSave : MonoBehaviour
{
    private List<InputField> inputFields = new List<InputField>();
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private List<Button> resetButtons = new List<Button>();
    private CharacterSceneData sceneData;
    private string characterId;
    private string sceneName;
    private bool isLoadingSceneData;
    private readonly SceneSaveController sceneFields = new SceneSaveController();
    private readonly CharacterNameFieldService characterNameField = new CharacterNameFieldService();

    private void Start()
    {
        DndSaveManager.EnsureExists();
        CharacterData character = DndSaveManager.Instance.EnsureActiveCharacter();
        characterId = character.id;
        sceneName = DndSaveManager.Instance.GetActiveSceneDataName();
        sceneData = character.GetSceneData(sceneName);
        CacheSceneControls();
        DoubleClickInputFieldActivator.ConfigureSceneInputs();
        CharacterSceneUiService.EnsurePortraitManager(gameObject);
        LoadSceneDataToUi();
        DeathSaveToggleSequence.ConfigureScene();
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
        if (isLoadingSceneData)
            return;

        CharacterSceneData saved = CharacterSceneSaveService.Save(
            DndSaveManager.Instance, characterId, sceneName, sceneFields, SaveIdentityAndSharedInputs);
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
            characterNameField.Load(DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null);
        }
        finally
        {
            isLoadingSceneData = false;
        }

        CharacterSceneUiService.RefreshAfterLoad();
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
                CharacterSceneUiService.DisablePersistentOnClick(resetButton);
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
        CharacterSceneUiService.ResetSceneWidgets();
        SaveSceneData();
        CharacterSceneUiService.RefreshAfterReset();
    }


    private void LoadSharedCharacterInputs()
    {
        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        SharedCharacterInputService.Load(character, inputFields, tmpInputFields);
    }

    private void ClearSharedCharacterInputs()
    {
        CharacterData character = DndSaveManager.Instance != null ? DndSaveManager.Instance.GetCharacter(characterId) : null;
        SharedCharacterInputService.Clear(character, inputFields, tmpInputFields);
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
