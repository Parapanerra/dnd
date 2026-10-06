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
