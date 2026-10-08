// Copies pre-scene character fields into the first character-sheet scene when needed.
public static class CharacterSceneMigrationService
{
    public static void CopyLegacyFieldsIfEmpty(CharacterData character, CharacterSceneData sceneData)
    {
        if (character == null || sceneData == null ||
            sceneData.inputData.Count > 0 || sceneData.toggleData.Count > 0 ||
            sceneData.sliderData.Count > 0 || sceneData.dropdownData.Count > 0)
            return;

        sceneData.inputData.AddRange(character.inputData);
        sceneData.toggleData.AddRange(character.toggleData);
        sceneData.sliderData.AddRange(character.sliderData);
        sceneData.dropdownData.AddRange(character.dropdownData);
    }
}
