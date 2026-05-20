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
            return "cartaPersonaj";

        if (Regex.IsMatch(sceneName, @"^informForPerson( \d+)?$"))
            return "informForPerson";

        if (Regex.IsMatch(sceneName, @"^inventory( \d+)?$"))
            return "inventory";

        if (Regex.IsMatch(sceneName, @"^Spels( \d+)?$"))
            return "Spels";

        if (Regex.IsMatch(sceneName, @"^spelBook( \d+)?$"))
            return "spelBook";

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
        if (Regex.IsMatch(sceneName, @"^petsesn( [1-7])?$"))
            return "petsesn";

        return sceneName;
    }

    private string GetLegacySceneName(int buildIndex)
    {
        switch (buildIndex)
        {
            case 0:
                return "menu";
            case 1:
            case 10:
            case 11:
            case 12:
            case 13:
                return "cartaPersonaj";
            case 2:
            case 17:
                return "petsesn";
            case 3:
            case 16:
            case 23:
            case 30:
            case 37:
                return "Spels";
            case 4:
            case 18:
                return "petsesn 1";
            case 5:
            case 19:
                return "petsesn 2";
            case 6:
            case 20:
                return "petsesn 3";
            case 7:
            case 15:
            case 22:
            case 29:
            case 36:
                return "inventory";
            case 8:
            case 14:
            case 21:
            case 28:
            case 35:
                return "informForPerson";
            case 9:
                return "zapisnuk";
            case 24:
            case 31:
            case 38:
            case 49:
            case 53:
                return "petsesn 4";
            case 25:
            case 32:
            case 39:
            case 50:
            case 54:
                return "petsesn 5";
            case 26:
            case 33:
            case 40:
            case 51:
            case 55:
                return "petsesn 6";
            case 27:
            case 34:
            case 41:
            case 52:
            case 56:
                return "petsesn 7";
            case 42:
                return "avtoru";
            case 43:
                return "proApk";
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
                return "spelBook";
            case 57:
            case 61:
            case 65:
                return "petsesn 4";
            case 58:
            case 62:
            case 66:
                return "petsesn 5";
            case 59:
            case 63:
            case 67:
                return "petsesn 6";
            case 60:
            case 64:
            case 68:
                return "petsesn 7";
            default:
                return "";
        }
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
            return "petsesn";

        int duplicateStart = 1 + duplicateGroup * 4;
        if (!int.TryParse(petMatch.Groups[1].Value, out int duplicatePageNumber))
            return "petsesn";

        int canonicalPageNumber = duplicatePageNumber - duplicateStart;
        if (canonicalPageNumber <= 0)
            return "petsesn";

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
