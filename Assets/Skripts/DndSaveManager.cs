using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.SceneManagement;

public class DndSaveManager : MonoBehaviour
{
    public static DndSaveManager Instance { get; private set; }

    public AppSaveData saveData;
    private string pendingSceneDataName;
    private string currentSceneDataName;
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
            TaruckDataValidator.Validate(imported);
            // Clone before assigning anything to the running session.
            AppSaveData candidate = TaruckBinaryCodec.DecodeFullSave(
                TaruckBinaryCodec.EncodeFullSave(merge ? saveData : imported, Application.version));
            if (merge)
            {
                foreach (CharacterData source in imported.characters)
                {
                    CharacterData copy = TaruckBinaryCodec.DecodeCharacterExport(
                        TaruckBinaryCodec.EncodeCharacterExport(source, Application.version));
                    if (candidate.characters.Exists(item => item.id == copy.id))
                        copy.id = Guid.NewGuid().ToString();
                    candidate.characters.Add(copy);
                }
                if (string.IsNullOrEmpty(candidate.lastActiveCharacterId) && candidate.characters.Count > 0)
                    candidate.lastActiveCharacterId = candidate.characters[0].id;
            }
            TaruckDataValidator.Validate(candidate);
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
