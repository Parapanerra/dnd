using System;
using System.IO;
using SimpleFileBrowser;
using UnityEngine;

public static class TaruckCharacterTransfer
{
    public static AppSaveData ReadCollection(string path, out bool binary, out byte[] bytes)
    {
        bytes = TaruckTransferFileReader.Read(path);
        binary = TaruckTransferFileUtility.HasMagic(bytes);
        return binary
            ? TaruckBinaryCodec.DecodeFullSave(bytes)
            : LegacyJsonImporter.FullSave(LegacyJsonImporter.DecodeFile(bytes));
    }

    public static CharacterData ReadCharacter(string path, out byte[] bytes)
    {
        bytes = TaruckTransferFileReader.Read(path);
        return TaruckTransferFileUtility.HasMagic(bytes)
            ? TaruckBinaryCodec.DecodeCharacterExport(bytes)
            : LegacyJsonImporter.Character(LegacyJsonImporter.DecodeFile(bytes));
    }

    public static void WriteCollection(string path, AppSaveData data, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(TaruckTransferFileUtility.EnsureExtension(path, ".tall"),
            TaruckBinaryCodec.EncodeFullSave(data, version));
    }

    public static void WriteCharacter(string path, CharacterData character, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(TaruckTransferFileUtility.EnsureExtension(path, ".tchar"),
            TaruckBinaryCodec.EncodeCharacterExport(character, version));
    }

    public static string ImportCharacter(DndSaveManager manager, CharacterData imported, string version)
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

    private static string MakeImportedCharacterName(DndSaveManager manager, string baseName)
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

    private static bool CharacterNameExists(DndSaveManager manager, string name)
    {
        if (manager == null || manager.saveData == null || manager.saveData.characters == null)
            return false;
        foreach (CharacterData character in manager.saveData.characters)
            if (character != null && string.Equals(character.characterName, name, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
