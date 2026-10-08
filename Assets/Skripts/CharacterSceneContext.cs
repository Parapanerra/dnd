using UnityEngine;
using UnityEngine.SceneManagement;

// Tracks which character scene is active and attaches its save controller.
public sealed class CharacterSceneContext
{
    private string pendingSceneDataName;
    private string currentSceneDataName;

    public void OnSceneLoaded(Scene scene)
    {
        currentSceneDataName = !string.IsNullOrWhiteSpace(pendingSceneDataName)
            ? pendingSceneDataName : scene.name;
        pendingSceneDataName = "";

        if (!IsCharacterSheetScene(scene.name))
            return;

        if (Object.FindAnyObjectByType<CharacterSheetManagerScene1>() != null)
            return;

        if (Object.FindAnyObjectByType<CharacterSceneAutoSave>() == null)
            new GameObject("CharacterSceneAutoSave").AddComponent<CharacterSceneAutoSave>();
    }

    public void SetPendingSceneDataName(string sceneName)
    {
        pendingSceneDataName = sceneName;
    }

    public void SetActiveSceneDataName(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            currentSceneDataName = sceneName;
    }

    public string GetActiveSceneDataName()
    {
        return !string.IsNullOrWhiteSpace(currentSceneDataName)
            ? currentSceneDataName : SceneManager.GetActiveScene().name;
    }

    private static bool IsCharacterSheetScene(string sceneName)
    {
        return sceneName.Contains("cartaPersonaj") ||
               sceneName.Contains("inventory") ||
               sceneName.Contains("informForPerson") ||
               sceneName.Contains("Spels") ||
               sceneName.Contains("spelBook") ||
               sceneName.Contains("petsesn");
    }
}
