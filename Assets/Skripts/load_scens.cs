using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

public class load_scenes : MonoBehaviour
{
    public void LoadScenes(int level)
    {
        DndSaveManager.EnsureExists();

        string legacySceneName = GetLegacySceneName(level);
        if (!string.IsNullOrEmpty(legacySceneName))
        {
            LoadSceneByName(legacySceneName);
            return;
        }

        string sceneName = GetCanonicalSceneName(level);
        if (!string.IsNullOrEmpty(sceneName))
        {
            LoadSceneByName(sceneName);
            return;
        }

        SceneManager.LoadScene(level);
    }

    public void Exit()
    {
        Application.Quit();
    }

    private string GetCanonicalSceneName(int buildIndex)
    {
        if (buildIndex < 0 || buildIndex >= SceneManager.sceneCountInBuildSettings)
            return "";

        string scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        string normalizedSceneName = NormalizeSceneName(sceneName);
        if (normalizedSceneName != sceneName)
            return normalizedSceneName;

        string canonicalPetScene = GetCanonicalPetSceneName(scenePath, sceneName);
        if (!string.IsNullOrEmpty(canonicalPetScene))
            return canonicalPetScene;

        return sceneName;
    }

    public static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "";

        sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName.Trim());

        if (Regex.IsMatch(sceneName, @"^cartaPersonaj( \d+)?$"))
            return AppConfig.Scenes.CharacterSheet;

        if (Regex.IsMatch(sceneName, @"^informForPerson( \d+)?$"))
            return AppConfig.Scenes.CharacterInfo;

        if (Regex.IsMatch(sceneName, @"^inventory( \d+)?$"))
            return AppConfig.Scenes.Inventory;

        if (Regex.IsMatch(sceneName, @"^Spels( \d+)?$"))
            return AppConfig.Scenes.Spells;

        if (Regex.IsMatch(sceneName, @"^spelBook( \d+)?$"))
            return AppConfig.Scenes.Spellbook;

        return sceneName;
    }

    public static void LoadSceneByName(string sceneName)
    {
        DndSaveManager.EnsureExists();
        sceneName = NormalizeSceneName(sceneName);
        if (string.IsNullOrEmpty(sceneName))
            return;

        string sceneToLoad = GetVisualSceneName(sceneName);
        if (sceneToLoad != sceneName)
            DndSaveManager.Instance.SetPendingSceneDataName(sceneName);

        SceneManager.LoadScene(sceneToLoad);
    }

    public static string GetVisualSceneName(string sceneName)
    {
        if (Regex.IsMatch(sceneName, @"^petsesn( [1-" + AppConfig.Scenes.WildShapePageCount + @"])?$"))
            return AppConfig.Scenes.WildShape;

        return sceneName;
    }

    private string GetLegacySceneName(int buildIndex)
    {
        return AppConfig.Scenes.TryGetLegacySceneName(buildIndex, out string sceneName)
            ? sceneName
            : "";
    }

    private string GetCanonicalPetSceneName(string scenePath, string sceneName)
    {
        Match petMatch = Regex.Match(sceneName, @"^petsesn(?: (\d+))?$");
        if (!petMatch.Success)
            return "";

        int duplicateGroup = GetDuplicateCharacterGroup(scenePath);
        if (duplicateGroup == 0)
            return sceneName;

        if (!petMatch.Groups[1].Success)
            return AppConfig.Scenes.WildShape;

        int duplicateStart = 1 + duplicateGroup * AppConfig.Scenes.WildShapePagesPerCharacterGroup;
        if (!int.TryParse(petMatch.Groups[1].Value, out int duplicatePageNumber))
            return AppConfig.Scenes.WildShape;

        int canonicalPageNumber = duplicatePageNumber - duplicateStart;
        if (canonicalPageNumber <= 0)
            return AppConfig.Scenes.WildShape;

        return "petsesn " + canonicalPageNumber;
    }

    private int GetDuplicateCharacterGroup(string scenePath)
    {
        Match match = Regex.Match(scenePath.Replace("\\", "/"), @"/personag(\d+)/");
        if (!match.Success)
            return 0;

        return int.TryParse(match.Groups[1].Value, out int group) ? group : 0;
    }
}
