using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class DndSaveManager : MonoBehaviour
{
    public static DndSaveManager Instance { get; private set; }

    public AppSaveData saveData;
    private readonly CharacterSceneContext sceneContext = new CharacterSceneContext();
    private float lastCreateCharacterTime = -AppConfig.SaveData.CharacterCreateDebounceSeconds;

    private string FilePath => Path.Combine(Application.persistentDataPath, "DndCharactersData.taruck-data");
    private string LegacyFilePath => Path.Combine(Application.persistentDataPath, "DndCharactersData.json");
    private bool saveBlocked;
    private readonly AutoSaveWriteGate autoSave = new AutoSaveWriteGate();
    public string SaveError { get; private set; }
    public int SuccessfulDiskWrites { get; private set; }

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
        {
            FlushPendingSave();
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void Update()
    {
        if (autoSave.IsDue(Time.unscaledTime, AppConfig.SaveData.AutoSaveDebounceSeconds))
            FlushPendingSave();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
            FlushPendingSave();
    }

    private void OnApplicationQuit()
    {
        FlushPendingSave();
    }

    public void RequestSaveData()
    {
        if (saveBlocked)
            return;

        autoSave.Request(Time.unscaledTime);
    }

    public void FlushPendingSave()
    {
        if (autoSave.Pending)
            SaveData();
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
        TaruckLocalLoadResult result = TaruckLocalRepository.Load(FilePath, LegacyFilePath, Application.version);
        if (result.Data != null)
            saveData = result.Data;
        saveBlocked = !result.CanWrite;
        autoSave.MarkSaved();
        SaveError = result.Error != null ? result.Error.Message : null;
        if (result.Error != null)
            Debug.LogError("DnD save load warning: " + result.Error.Message);

        NormalizeSaveData();

        if (saveBlocked)
            Debug.LogError("DnD save writes are blocked until a valid local save is restored: " + SaveError);
    }

    public void SaveData()
    {
        if (saveBlocked)
        {
            Debug.LogError("DnD save write blocked: " + SaveError);
            return;
        }
        NormalizeSaveData();

        try
        {
            TaruckLocalRepository.Save(FilePath, saveData, Application.version);
            SuccessfulDiskWrites++;
#if DEVELOPMENT_BUILD
            Debug.Log("TaruckDiskWrite #" + SuccessfulDiskWrites);
#endif
            autoSave.MarkSaved();
            SaveError = null;
        }
        catch (Exception exception)
        {
            Debug.LogError("Could not save DnD data: " + exception.Message);
            SaveError = exception.Message;
            autoSave.MarkFailed(Time.unscaledTime, 5f);
        }
    }

    public bool TryImportCollection(AppSaveData imported, bool merge, out string error)
    {
        error = null;
        if (saveBlocked)
        {
            error = SaveError ?? "Локальне сховище недоступне";
            return false;
        }

        try
        {
            AppSaveData candidate = CharacterCollectionImportService.Prepare(
                saveData, imported, merge, Application.version);
            if (File.Exists(FilePath))
                File.Copy(FilePath, FilePath + ".preimport.bak", true);
            TaruckLocalRepository.Save(FilePath, candidate, Application.version);
            saveData = candidate;
            autoSave.MarkSaved();
            SaveError = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            SaveError = error;
            Debug.LogError("DnD collection import failed: " + error);
            return false;
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

        CharacterData newChar = CharacterCollectionService.Create(saveData);
        SaveData();
        return newChar;
    }

    public CharacterData GetCharacter(string id)
    {
        return CharacterCollectionService.Find(saveData, id);
    }

    public bool SetActiveCharacter(string id)
    {
        CharacterData character = GetCharacter(id);
        if (character == null)
        {
            Debug.LogError("Cannot set active character. Character not found: " + id);
            return false;
        }

        CharacterCollectionService.Select(saveData, id);
        SaveData();
        Debug.Log("Active DnD character: " + character.characterName + " (" + id + ")");
        return true;
    }
    
    public void DeleteCharacter(string id)
    {
        if (CharacterCollectionService.Delete(saveData, id))
        {
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
        sceneContext.OnSceneLoaded(scene);
    }

    public void SetPendingSceneDataName(string sceneName)
    {
        sceneContext.SetPendingSceneDataName(sceneName);
    }

    public void SetActiveSceneDataName(string sceneName)
    {
        sceneContext.SetActiveSceneDataName(sceneName);
    }

    public string GetActiveSceneDataName()
    {
        return sceneContext.GetActiveSceneDataName();
    }

    public void NormalizeSaveData()
    {
        saveData = SaveDataNormalizer.Normalize(saveData);
    }
}
