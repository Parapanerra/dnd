using System;

// Mutates character membership and selection in memory; persistence stays with DndSaveManager.
internal sealed class CharacterCollectionService
{
    private readonly AppSaveData data;

    internal CharacterCollectionService(AppSaveData data)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
    }

    internal CharacterData Create()
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

    internal CharacterData Find(string id)
    {
        return data.characters.Find(character => character.id == id);
    }

    internal bool Select(string id)
    {
        if (Find(id) == null)
            return false;

        data.lastActiveCharacterId = id;
        return true;
    }

    internal bool Delete(string id)
    {
        CharacterData character = Find(id);
        if (character == null)
            return false;

        data.characters.Remove(character);
        if (data.lastActiveCharacterId == id)
            data.lastActiveCharacterId = data.characters.Count > 0 ? data.characters[0].id : "";
        return true;
    }
}
