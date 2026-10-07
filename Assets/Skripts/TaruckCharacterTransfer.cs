using System;
using System.IO;
using System.Text;
using SimpleFileBrowser;
using UnityEngine;

public static class TaruckCharacterTransfer
{
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TARUCKPK");

    public static AppSaveData ReadCollection(string path, out bool binary, out byte[] bytes)
    {
        bytes = TaruckTransferFileReader.Read(path);
        binary = HasMagic(bytes);
        return binary
            ? TaruckBinaryCodec.DecodeFullSave(bytes)
            : LegacyJsonImporter.FullSave(LegacyJsonImporter.DecodeFile(bytes));
    }

    public static CharacterData ReadCharacter(string path, out byte[] bytes)
    {
        bytes = TaruckTransferFileReader.Read(path);
        return HasMagic(bytes)
            ? TaruckBinaryCodec.DecodeCharacterExport(bytes)
            : LegacyJsonImporter.Character(LegacyJsonImporter.DecodeFile(bytes));
    }

    public static void WriteCollection(string path, AppSaveData data, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(EnsureExtensionForFileBrowserPath(path, ".tall"),
            TaruckBinaryCodec.EncodeFullSave(data, version));
    }

    public static void WriteCharacter(string path, CharacterData character, string version)
    {
        FileBrowserHelpers.WriteBytesToFile(EnsureExtensionForFileBrowserPath(path, ".tchar"),
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

    public static bool HasMagic(byte[] bytes)
    {
        if (bytes == null || bytes.Length < Magic.Length)
            return false;
        for (int i = 0; i < Magic.Length; i++)
            if (bytes[i] != Magic[i])
                return false;
        return true;
    }

    public static string EnsureExtensionForFileBrowserPath(string path, string extension)
    {
        if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
            return path;

        if (!extension.StartsWith(".", StringComparison.Ordinal))
            extension = "." + extension;
        if (path.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
            return path;

        string[] oldExtensions = { ".taruck-save", ".taruck-character", ".taruck-item" };
        foreach (string oldExtension in oldExtensions)
            if (path.EndsWith(oldExtension, StringComparison.OrdinalIgnoreCase))
                return path.Substring(0, path.Length - oldExtension.Length) + extension;

        return path + extension;
    }

    public static string DefaultFileBrowserPath()
    {
        string[] candidates =
        {
            "/storage/emulated/0/Download",
            "/sdcard/Download",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        foreach (string candidate in candidates)
            if (!string.IsNullOrEmpty(candidate) && Directory.Exists(candidate))
                return candidate;

        return null;
    }

    public static string SafeFileName(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            value = fallback;
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
            value = value.Replace(invalidChar, '_');
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
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
