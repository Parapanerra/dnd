using UnityEngine;
using UnityEngine.SceneManagement;

// Installs the scene-specific page controller once, including on a direct scene launch.
internal sealed class ScenePageControllerInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        new ScenePageControllerInstaller().InstallActiveScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        new ScenePageControllerInstaller().InstallActiveScene();
    }

    private void InstallActiveScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == AppConfig.Scenes.Inventory)
            EnsureComponent<InventoryPageManager>("InventoryPageManager");
        else if (sceneName == AppConfig.Scenes.Spellbook)
            EnsureComponent<SpellbookPageSwitcher>("SpellbookPageSwitcher");
    }

    private void EnsureComponent<T>(string objectName) where T : Component
    {
        if (Object.FindAnyObjectByType<T>() != null)
            return;

        GameObject controllerObject = new GameObject(objectName);
        controllerObject.AddComponent<T>();
    }
}
