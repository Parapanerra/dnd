using System;
using System.IO;
using System.Text;
using UnityEngine;

// The only production entry point for imported JSON. New exports and local saves use TaruckBinaryCodec.
public static class LegacyJsonImporter
{
    public static string DecodeFile(byte[] bytes)
    {
        if (bytes == null || bytes.Length > 128 * 1024 * 1024)
            throw new InvalidDataException("Invalid legacy file size");
        return new UTF8Encoding(false, true).GetString(bytes);
    }

    public static AppSaveData FullSave(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"characters\""))
            throw new InvalidDataException("Not a legacy full save");
        AppSaveData data = JsonUtility.FromJson<AppSaveData>(json);
        if (data == null || data.characters == null) throw new InvalidDataException("Invalid legacy full save");
        return SaveMigrationService.FromLegacy(data);
    }

    public static CharacterData Character(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Empty character file");
        CharacterData character;
        if (json.Contains("\"character\""))
        {
            CharacterExportData export = JsonUtility.FromJson<CharacterExportData>(json);
            if (export == null || export.version > 1) throw new InvalidDataException("Unsupported legacy character version");
            character = export.character;
        }
        else if (json.Contains("\"id\"")) character = JsonUtility.FromJson<CharacterData>(json);
        else throw new InvalidDataException("Not a legacy character file");
        if (character == null) throw new InvalidDataException("Invalid legacy character");
        var wrapper = new AppSaveData();
        wrapper.characters.Add(character);
        SaveDataNormalizer.Normalize(wrapper);
        TaruckDataValidator.Validate(character);
        return character;
    }

    public static InventoryItemExportData Item(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.Contains("\"itemName\""))
            throw new InvalidDataException("Not a legacy item file");
        InventoryItemExportData item = JsonUtility.FromJson<InventoryItemExportData>(json);
        if (item == null) throw new InvalidDataException("Invalid legacy item");
        if (!string.IsNullOrEmpty(item.customImageBase64))
        {
            byte[] image = Convert.FromBase64String(item.customImageBase64);
            if (image.Length > 16 * 1024 * 1024) throw new InvalidDataException("Item image too large");
        }
        return item;
    }
}
