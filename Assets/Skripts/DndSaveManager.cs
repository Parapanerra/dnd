using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class DndSaveManager : MonoBehaviour
{
    private bool initialized;
    internal bool IsInitialized => initialized;

    [SerializeField] private AppSaveData saveData;
    private readonly CharacterSceneContext sceneContext = new CharacterSceneContext();
    private readonly CharacterSceneStateService sceneState = new CharacterSceneStateService();
    private readonly SaveDataNormalizer normalizer = new SaveDataNormalizer();
    private readonly TaruckLocalRepository repository = new TaruckLocalRepository();
    private CharacterCollectionService characters;
    private float lastCreateCharacterTime = -AppConfig.SaveData.CharacterCreateDebounceSeconds;

    private string FilePath => Path.Combine(Application.persistentDataPath, "DndCharactersData.taruck-data");
    private string LegacyFilePath => Path.Combine(Application.persistentDataPath, "DndCharactersData.json");
    private bool saveBlocked;
    private readonly AutoSaveWriteGate autoSave = new AutoSaveWriteGate();
    internal string SaveError { get; private set; }
    internal bool CanWrite => !saveBlocked;
    private int SuccessfulDiskWrites { get; set; }
    internal bool HasPendingSave => autoSave.Pending;
    private string deleteAfterMenuLoadId;

    private void Awake()
    {
        DndSaveManager existing = new RuntimeManagerResolver().FindSaveManager();
        if (existing != null && existing != this && existing.initialized)
        {
            Destroy(gameObject);
            return;
        }
        
        initialized = true;
        DontDestroyOnLoad(gameObject);
        LoadData();
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        if (initialized)
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

    internal void RequestSaveData()
    {
        if (saveBlocked)
            return;

        autoSave.Request(Time.unscaledTime);
    }

    internal void FlushPendingSave()
    {
        if (autoSave.Pending)
            SaveData();
    }

    private void LoadData()
    {
        TaruckLocalLoadResult result = repository.Load(FilePath, LegacyFilePath, Application.version);
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

    internal void SaveData()
    {
        if (saveBlocked)
        {
            Debug.LogError("DnD save write blocked: " + SaveError);
            return;
        }
        NormalizeSaveData();

        try
        {
            repository.Save(FilePath, saveData, Application.version);
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

    internal bool TryImportCollection(AppSaveData imported, bool merge, out string error)
    {
        error = null;
        if (saveBlocked)
        {
            error = SaveError ?? "Локальне сховище недоступне";
            return false;
        }

        try
        {
            var importer = new CharacterCollectionImportService(repository, FilePath, Application.version);
            AppSaveData candidate = importer.Import(saveData, imported, merge);
            saveData = candidate;
            characters = new CharacterCollectionService(saveData);
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

    internal CharacterData CreateNewCharacter()
    {
        if (Time.unscaledTime - lastCreateCharacterTime <
            AppConfig.SaveData.CharacterCreateDebounceSeconds)
        {
            CharacterData activeCharacter = GetActiveCharacter();
            if (activeCharacter != null)
                return activeCharacter;
        }

        lastCreateCharacterTime = Time.unscaledTime;

        CharacterData newChar = characters.Create();
        SaveData();
        return newChar;
    }

    internal int GetCharacterCount()
    {
        return saveData?.characters?.Count ?? 0;
    }

    internal CharacterData[] GetCharactersSnapshot()
    {
        return saveData?.characters?.ToArray() ?? Array.Empty<CharacterData>();
    }

    internal CharacterData GetCharacterAt(int index)
    {
        return index >= 0 && index < GetCharacterCount() ? saveData.characters[index] : null;
    }

    internal string GetActiveCharacterId()
    {
        return saveData?.lastActiveCharacterId ?? "";
    }

    internal AppSaveData GetCollectionForExport()
    {
        return saveData;
    }
    internal CharacterData GetCharacter(string id)
    {
        return characters.Find(id);
    }

    internal bool SetActiveCharacter(string id)
    {
        CharacterData character = GetCharacter(id);
        if (character == null)
        {
            Debug.LogError("Cannot set active character. Character not found: " + id);
            return false;
        }

        characters.Select(id);
        SaveData();
        Debug.Log("Active DnD character: " + character.characterName + " (" + id + ")");
        return true;
    }
    
    internal void DeleteCharacter(string id)
    {
        if (characters.Delete(id))
        {
            SaveData();
        }
    }

    internal bool ReturnToMenuThenDeleteCharacter(string id)
    {
        if (!CanWrite || string.IsNullOrEmpty(id) || GetCharacter(id) == null)
            return false;

        deleteAfterMenuLoadId = id;
        try
        {
            SceneManager.LoadScene(AppConfig.Scenes.Menu);
            return true;
        }
        catch (Exception exception)
        {
            deleteAfterMenuLoadId = null;
            Debug.LogError("Could not open menu before deleting character: " + exception.Message);
            return false;
        }
    }

    internal CharacterData GetActiveCharacter()
    {
        if (saveData == null)
        {
            LoadData();
        }

        return GetCharacter(saveData.lastActiveCharacterId);
    }

    internal CharacterData EnsureActiveCharacter()
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

        // The menu must stay empty until the user explicitly creates a character.
        if (SceneManager.GetActiveScene().name == AppConfig.Scenes.Menu)
            return null;

        return CreateNewCharacter();
    }

    internal CharacterSceneData GetActiveSceneData(bool createIfMissing = true)
    {
        CharacterData activeCharacter = EnsureActiveCharacter();
        return activeCharacter.GetSceneData(GetActiveSceneDataName(), createIfMissing);
    }

    internal CharacterSceneData GetSceneDataForCharacter(string characterId, string sceneName, bool createIfMissing = true)
    {
        CharacterData character = GetCharacter(characterId);
        if (character == null)
            return null;

        return character.GetSceneData(sceneName, createIfMissing);
    }

    internal void ClearSceneDataFamilyForCharacter(string characterId, string sceneName)
    {
        CharacterData character = GetCharacter(characterId);
        if (character == null)
            return;

        sceneState.ClearFamily(character, sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneContext.OnSceneLoaded(scene);
        if (scene.name != AppConfig.Scenes.Menu || string.IsNullOrEmpty(deleteAfterMenuLoadId))
            return;

        string id = deleteAfterMenuLoadId;
        deleteAfterMenuLoadId = null;
        DeleteCharacter(id);
    }

    internal void SetPendingSceneDataName(string sceneName)
    {
        sceneContext.SetPendingSceneDataName(sceneName);
    }

    internal void SetActiveSceneDataName(string sceneName)
    {
        sceneContext.SetActiveSceneDataName(sceneName);
    }

    internal string GetActiveSceneDataName()
    {
        return sceneContext.GetActiveSceneDataName();
    }

    private void NormalizeSaveData()
    {
        AppSaveData normalized = normalizer.Normalize(saveData);
        if (!ReferenceEquals(normalized, saveData) || characters == null)
            characters = new CharacterCollectionService(normalized);
        saveData = normalized;
    }
}
