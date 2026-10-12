using UnityEngine.SceneManagement;

// Performs navigation and keeps the save scene identity when a visual scene is reused.
internal sealed class SceneNavigator
{
    private readonly RuntimeManagerResolver managers = new RuntimeManagerResolver();

    internal void LoadSceneByName(string sceneName)
    {
        DndSaveManager saveManager = managers.SaveManager();
        sceneName = load_scenes.NormalizeSceneName(sceneName);
        if (string.IsNullOrEmpty(sceneName))
            return;

        string sceneToLoad = load_scenes.GetVisualSceneName(sceneName);
        if (sceneToLoad != sceneName)
            saveManager.SetPendingSceneDataName(sceneName);

        SceneManager.LoadScene(sceneToLoad);
    }
}
