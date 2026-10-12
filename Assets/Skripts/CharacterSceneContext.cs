using UnityEngine;
using UnityEngine.SceneManagement;

// Tracks which character scene is active and attaches its save controller.
internal sealed class CharacterSceneContext
{
    private string pendingSceneDataName;
    private string currentSceneDataName;

    internal void OnSceneLoaded(Scene scene)
    {
        currentSceneDataName = !string.IsNullOrWhiteSpace(pendingSceneDataName)
            ? pendingSceneDataName : scene.name;
        pendingSceneDataName = "";

        if (!IsCharacterSheetScene(scene.name))
            return;

        if (Object.FindAnyObjectByType<CharacterSheetManagerScene1>() == null)
            new GameObject("CharacterSheetController").AddComponent<CharacterSheetManagerScene1>();
    }

    internal void SetPendingSceneDataName(string sceneName)
    {
        pendingSceneDataName = sceneName;
    }

    internal void SetActiveSceneDataName(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            currentSceneDataName = sceneName;
    }

    internal string GetActiveSceneDataName()
    {
        return !string.IsNullOrWhiteSpace(currentSceneDataName)
            ? currentSceneDataName : SceneManager.GetActiveScene().name;
    }

    private static bool IsCharacterSheetScene(string sceneName)
    {
        return sceneName == AppConfig.Scenes.CharacterSheet ||
               sceneName == AppConfig.Scenes.Inventory ||
               sceneName == AppConfig.Scenes.CharacterInfo ||
               sceneName == AppConfig.Scenes.Spells ||
               sceneName == AppConfig.Scenes.Spellbook ||
               sceneName == AppConfig.Scenes.WildShape;
    }
}
