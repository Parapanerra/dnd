using System;
using System.Collections.Generic;
using System.IO;

public sealed class SaveDataNormalizer
{
    public AppSaveData Normalize(AppSaveData saveData)
    {
        if (saveData == null)
            saveData = new AppSaveData();

        if (saveData.characters == null)
            saveData.characters = new List<CharacterData>();

        foreach (CharacterData character in saveData.characters)
        {
            if (character == null)
                throw new InvalidDataException("Invalid character in save");

            if (string.IsNullOrEmpty(character.id))
                character.id = Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(character.characterName) || IsNumericName(character.characterName))
                character.characterName = "Новий персонаж";

            if (character.inputData == null)
                character.inputData = new List<string>();
            if (character.sharedStringData == null)
                character.sharedStringData = new List<StringSaveEntry>();
            if (character.toggleData == null)
                character.toggleData = new List<bool>();
            if (character.sliderData == null)
                character.sliderData = new List<float>();
            if (character.dropdownData == null)
                character.dropdownData = new List<int>();
            if (character.sceneStates == null)
                character.sceneStates = new List<CharacterSceneData>();
            if (character.albumImages == null)
                character.albumImages = new List<AlbumImageData>();

            foreach (CharacterSceneData sceneData in character.sceneStates)
            {
                if (sceneData == null)
                    throw new InvalidDataException("Invalid scene in save");

                if (sceneData.inputData == null)
                    sceneData.inputData = new List<string>();
                if (sceneData.toggleData == null)
                    sceneData.toggleData = new List<bool>();
                if (sceneData.sliderData == null)
                    sceneData.sliderData = new List<float>();
                if (sceneData.dropdownData == null)
                    sceneData.dropdownData = new List<int>();
                if (sceneData.stringData == null)
                    sceneData.stringData = new List<StringSaveEntry>();
                if (sceneData.intData == null)
                    sceneData.intData = new List<IntSaveEntry>();
                if (sceneData.floatData == null)
                    sceneData.floatData = new List<FloatSaveEntry>();
            }
        }

        return saveData;
    }

    private bool IsNumericName(string value)
    {
        return float.TryParse(value, out _);
    }
}
