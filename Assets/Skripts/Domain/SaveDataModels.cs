using System;
using System.Collections.Generic;

[Serializable]
public class CharacterData
{
    public string id;
    public string characterName = "Новий персонаж";
    public int maxHealth;
    public int currentHealth;

    // Legacy fields remain until every old save has migrated to stable field IDs.
    public List<string> inputData = new List<string>();
    public List<bool> toggleData = new List<bool>();
    public List<float> sliderData = new List<float>();
    public List<int> dropdownData = new List<int>();
    public List<CharacterSceneData> sceneStates = new List<CharacterSceneData>();
    public List<StringSaveEntry> sharedStringData = new List<StringSaveEntry>();

    public CharacterData(string newId)
    {
        id = newId;
    }

    public CharacterSceneData GetSceneData(string sceneName, bool createIfMissing = true)
    {
        CharacterSceneData state = sceneStates.Find(item => item.sceneName == sceneName);
        if (state == null && createIfMissing)
        {
            state = new CharacterSceneData(sceneName);
            sceneStates.Add(state);
        }

        return state;
    }

    public string GetSharedString(string key, string defaultValue = "")
    {
        if (sharedStringData == null)
            sharedStringData = new List<StringSaveEntry>();

        StringSaveEntry entry = sharedStringData.Find(item => item.key == key);
        return entry != null ? entry.value : defaultValue;
    }

    public bool HasSharedString(string key)
    {
        if (sharedStringData == null)
            sharedStringData = new List<StringSaveEntry>();

        return sharedStringData.Exists(item => item.key == key);
    }

    public void SetSharedString(string key, string value)
    {
        if (sharedStringData == null)
            sharedStringData = new List<StringSaveEntry>();

        StringSaveEntry entry = sharedStringData.Find(item => item.key == key);
        if (entry == null)
        {
            entry = new StringSaveEntry { key = key };
            sharedStringData.Add(entry);
        }

        entry.value = value;
    }

    public void DeleteSharedString(string key)
    {
        if (sharedStringData != null)
            sharedStringData.RemoveAll(item => item.key == key);
    }
}

[Serializable]
public class CharacterSceneData
{
    public string sceneName;
    public List<string> inputData = new List<string>();
    public List<bool> toggleData = new List<bool>();
    public List<float> sliderData = new List<float>();
    public List<int> dropdownData = new List<int>();
    public List<StringSaveEntry> stringData = new List<StringSaveEntry>();
    public List<IntSaveEntry> intData = new List<IntSaveEntry>();
    public List<FloatSaveEntry> floatData = new List<FloatSaveEntry>();
    // Version of the stable-field reader used; presence of each key tracks partial migration.
    public int stableFieldVersion;

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
        floatData?.Clear();
        stableFieldVersion = 0;
    }
}

[Serializable]
public class StringSaveEntry
{
    public string key;
    public string value;
}

[Serializable]
public class IntSaveEntry
{
    public string key;
    public int value;
}

[Serializable]
public class FloatSaveEntry
{
    public string key;
    public float value;
}

[Serializable]
public class AppSaveData
{
    public string lastActiveCharacterId;
    public List<CharacterData> characters = new List<CharacterData>();
}

[Serializable]
public class CharacterExportData
{
    public int version = 1;
    public CharacterData character;
}

[Serializable]
public class InventoryItemExportData
{
    public string itemName;
    public string itemDescription;
    public int category;
    public int weaponIndex;
    public int armorIndex;
    public int bagsIndex;
    public int magicIndex;
    public int otherIndex;
    public int chegerIndex;
    public string customImageBase64;
}
