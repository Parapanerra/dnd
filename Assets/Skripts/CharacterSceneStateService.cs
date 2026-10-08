// Clears all saved pages that belong to the same scene family.
public static class CharacterSceneStateService
{
    public static void ClearFamily(CharacterData character, string sceneName)
    {
        if (character == null || character.sceneStates == null)
            return;

        string familyName = GetFamilyName(sceneName);
        foreach (CharacterSceneData state in character.sceneStates)
            if (state != null && GetFamilyName(state.sceneName) == familyName)
                state.ClearValues();
    }

    public static string GetFamilyName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "";

        sceneName = sceneName.Trim();
        int lastSpace = sceneName.LastIndexOf(' ');
        if (lastSpace > 0 && int.TryParse(sceneName.Substring(lastSpace + 1), out _))
            return sceneName.Substring(0, lastSpace);

        return sceneName;
    }
}
