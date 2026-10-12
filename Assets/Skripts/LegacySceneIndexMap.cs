using System.Collections.Generic;

// Values here are indices from the old Unity Build Settings list. Serialized
// LoadScenes(int) UnityEvents still pass these numbers. They are not indices
// into today's Build Settings and must keep their original destinations.
internal static class LegacySceneIndexMap
{
    private static readonly Dictionary<int, string> SceneNames = new Dictionary<int, string>
    {
        { 0, AppConfig.Scenes.Menu },
        { 1, AppConfig.Scenes.CharacterSheet }, { 10, AppConfig.Scenes.CharacterSheet }, { 11, AppConfig.Scenes.CharacterSheet },
        { 12, AppConfig.Scenes.CharacterSheet }, { 13, AppConfig.Scenes.CharacterSheet },
        { 2, AppConfig.Scenes.WildShape }, { 17, AppConfig.Scenes.WildShape },
        { 3, AppConfig.Scenes.Spells }, { 16, AppConfig.Scenes.Spells }, { 23, AppConfig.Scenes.Spells }, { 30, AppConfig.Scenes.Spells }, { 37, AppConfig.Scenes.Spells },
        { 4, AppConfig.Scenes.WildShape + " 1" }, { 18, AppConfig.Scenes.WildShape + " 1" },
        { 5, AppConfig.Scenes.WildShape + " 2" }, { 19, AppConfig.Scenes.WildShape + " 2" },
        { 6, AppConfig.Scenes.WildShape + " 3" }, { 20, AppConfig.Scenes.WildShape + " 3" },
        { 7, AppConfig.Scenes.Inventory }, { 15, AppConfig.Scenes.Inventory }, { 22, AppConfig.Scenes.Inventory },
        { 29, AppConfig.Scenes.Inventory }, { 36, AppConfig.Scenes.Inventory },
        { 8, AppConfig.Scenes.CharacterInfo }, { 14, AppConfig.Scenes.CharacterInfo }, { 21, AppConfig.Scenes.CharacterInfo },
        { 28, AppConfig.Scenes.CharacterInfo }, { 35, AppConfig.Scenes.CharacterInfo },
        { 9, AppConfig.Scenes.Notes },
        { 24, AppConfig.Scenes.WildShape + " 4" }, { 31, AppConfig.Scenes.WildShape + " 4" }, { 38, AppConfig.Scenes.WildShape + " 4" },
        { 49, AppConfig.Scenes.WildShape + " 4" }, { 53, AppConfig.Scenes.WildShape + " 4" },
        { 25, AppConfig.Scenes.WildShape + " 5" }, { 32, AppConfig.Scenes.WildShape + " 5" }, { 39, AppConfig.Scenes.WildShape + " 5" },
        { 50, AppConfig.Scenes.WildShape + " 5" }, { 54, AppConfig.Scenes.WildShape + " 5" },
        { 26, AppConfig.Scenes.WildShape + " 6" }, { 33, AppConfig.Scenes.WildShape + " 6" }, { 40, AppConfig.Scenes.WildShape + " 6" },
        { 51, AppConfig.Scenes.WildShape + " 6" }, { 55, AppConfig.Scenes.WildShape + " 6" },
        { 27, AppConfig.Scenes.WildShape + " 7" }, { 34, AppConfig.Scenes.WildShape + " 7" }, { 41, AppConfig.Scenes.WildShape + " 7" },
        { 52, AppConfig.Scenes.WildShape + " 7" }, { 56, AppConfig.Scenes.WildShape + " 7" },
        { 42, AppConfig.Scenes.Authors },
        { 43, AppConfig.Scenes.About },
        { 44, AppConfig.Scenes.Spellbook }, { 45, AppConfig.Scenes.Spellbook }, { 46, AppConfig.Scenes.Spellbook },
        { 47, AppConfig.Scenes.Spellbook }, { 48, AppConfig.Scenes.Spellbook },
        { 57, AppConfig.Scenes.WildShape + " 4" }, { 61, AppConfig.Scenes.WildShape + " 4" }, { 65, AppConfig.Scenes.WildShape + " 4" },
        { 58, AppConfig.Scenes.WildShape + " 5" }, { 62, AppConfig.Scenes.WildShape + " 5" }, { 66, AppConfig.Scenes.WildShape + " 5" },
        { 59, AppConfig.Scenes.WildShape + " 6" }, { 63, AppConfig.Scenes.WildShape + " 6" }, { 67, AppConfig.Scenes.WildShape + " 6" },
        { 60, AppConfig.Scenes.WildShape + " 7" }, { 64, AppConfig.Scenes.WildShape + " 7" }, { 68, AppConfig.Scenes.WildShape + " 7" }
    };

    internal static bool TryGetSceneName(int legacyBuildIndex, out string sceneName)
    {
        return SceneNames.TryGetValue(legacyBuildIndex, out sceneName);
    }
}