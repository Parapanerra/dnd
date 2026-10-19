using System;
using System.Collections.Generic;
using System.IO;

public static class TaruckDataValidator
{
    public static void Validate(AppSaveData data)
    {
        if (data == null || data.characters == null || data.characters.Count > 1000)
            throw new InvalidDataException("Invalid character collection");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (CharacterData character in data.characters)
        {
            Validate(character);
            if (!ids.Add(character.id)) throw new InvalidDataException("Duplicate character ID");
        }
        if (data.characters.Count > 0 && !ids.Contains(data.lastActiveCharacterId))
            throw new InvalidDataException("Active character is missing");
        if (data.characters.Count == 0 && !string.IsNullOrEmpty(data.lastActiveCharacterId))
            throw new InvalidDataException("Active character is missing");
    }

    public static void Validate(CharacterData character)
    {
        if (character == null || string.IsNullOrWhiteSpace(character.id) ||
            string.IsNullOrWhiteSpace(character.characterName) ||
            character.sceneStates == null || character.sceneStates.Count > 128)
            throw new InvalidDataException("Invalid character");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (CharacterSceneData scene in character.sceneStates)
        {
            if (scene == null || string.IsNullOrWhiteSpace(scene.sceneName) || !names.Add(scene.sceneName))
                throw new InvalidDataException("Invalid or duplicate scene name");
            Unique(scene.stringData, e => e.key);
            Unique(scene.intData, e => e.key);
            Unique(scene.floatData, e => e.key);
        }
        Unique(character.sharedStringData, e => e.key);
        if (character.albumImages == null || character.albumImages.Count > 260)
            throw new InvalidDataException("Invalid album image count");
        var imageIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (AlbumImageData image in character.albumImages)
        {
            if (image == null || !Guid.TryParse(image.id, out _) || !imageIds.Add(image.id) ||
                image.width < 1 || image.height < 1 || image.width > 8192 || image.height > 8192 ||
                (long)image.width * image.height > 32000000 || image.fileSize < 1 || image.fileSize > 16 * 1024 * 1024 ||
                image.createdAtUtcTicks < DateTime.MinValue.Ticks || image.createdAtUtcTicks > DateTime.MaxValue.Ticks ||
                image.sortOrder < 0 || image.pageIndex < 0 || image.pageIndex > 4 ||
                (image.title?.Length ?? 0) > 200 ||
                (image.description?.Length ?? 0) > 2000 || !IsSafeMediaPath(image.relativeFilePath) ||
                !IsSafeMediaPath(image.thumbnailRelativePath) ||
                string.IsNullOrEmpty(image.contentHash) || image.contentHash.Length != 64)
                throw new InvalidDataException("Invalid album image metadata");
        }
    }

    private static bool IsSafeMediaPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Contains("..") || path.Contains(":") ||
            path.Contains("\\") || path.StartsWith("/", StringComparison.Ordinal))
            return false;
        string[] parts = path.Split('/');
        if (parts.Length != 3 || parts[0] != "Media") return false;
        foreach (string part in parts)
            if (string.IsNullOrWhiteSpace(part)) return false;
        return true;
    }

    private static void Unique<T>(List<T> entries, Func<T, string> key) where T : class
    {
        if (entries == null) throw new InvalidDataException("Missing keyed collection");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (T entry in entries)
            if (entry == null || string.IsNullOrWhiteSpace(key(entry)) || !seen.Add(key(entry)))
                throw new InvalidDataException("Invalid or duplicate keyed entry");
    }
}
