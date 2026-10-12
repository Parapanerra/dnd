using System;
using System.IO;

// Adds one imported character without mixing collection mutation with file transfer.
internal sealed class CharacterSingleImportService
{
    internal string Import(DndSaveManager manager, CharacterData imported, string version)
    {
        CharacterData copy = TaruckBinaryCodec.DecodeCharacterExport(
            TaruckBinaryCodec.EncodeCharacterExport(imported, version));
        copy.id = Guid.NewGuid().ToString();
        copy.characterName = MakeImportedCharacterName(manager, copy.characterName);
        var collection = new AppSaveData { lastActiveCharacterId = copy.id };
        collection.characters.Add(copy);
        if (!manager.TryImportCollection(collection, true, out string error))
            throw new IOException(error);
        return copy.id;
    }

    private string MakeImportedCharacterName(DndSaveManager manager, string baseName)
    {
        baseName = string.IsNullOrWhiteSpace(baseName) ? "Імпортований персонаж" : baseName.Trim();
        if (!CharacterNameExists(manager, baseName))
            return baseName;

        int index = AppConfig.MainMenu.ImportedNameFirstSuffix;
        string candidate;
        do
        {
            candidate = baseName + " (" + index + ")";
            index++;
        }
        while (CharacterNameExists(manager, candidate));
        return candidate;
    }

    private bool CharacterNameExists(DndSaveManager manager, string name)
    {
        if (manager == null || manager.GetCharacterCount() == 0)
            return false;
        foreach (CharacterData character in manager.GetCharactersSnapshot())
            if (character != null && string.Equals(character.characterName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
