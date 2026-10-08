using System;

// Mutates character membership and selection in memory; persistence stays with DndSaveManager.
public static class CharacterCollectionService
{
    public static CharacterData Create(AppSaveData data)
    {
        string id = Guid.NewGuid().ToString();
        CharacterData character = new CharacterData(id)
        {
            characterName = "Новий персонаж " + (data.characters.Count + 1),
            maxHealth = 0,
            currentHealth = 0
        };
        data.characters.Add(character);
        data.lastActiveCharacterId = id;
        return character;
    }

    public static CharacterData Find(AppSaveData data, string id)
    {
        return data.characters.Find(character => character.id == id);
    }

    public static bool Select(AppSaveData data, string id)
    {
        if (Find(data, id) == null)
            return false;

        data.lastActiveCharacterId = id;
        return true;
    }

    public static bool Delete(AppSaveData data, string id)
    {
        CharacterData character = Find(data, id);
        if (character == null)
            return false;

        data.characters.Remove(character);
        if (data.lastActiveCharacterId == id)
            data.lastActiveCharacterId = data.characters.Count > 0 ? data.characters[0].id : "";
        return true;
    }
}
