using UnityEngine;

public class SceneSwitcher1 : MonoBehaviour
{
    public void SwitchScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(load_scenes.NormalizeSceneName(sceneName)))
        {
            load_scenes.LoadSceneByName(sceneName);
            return;
        }

        Debug.LogError("Invalid scene name. Scene name cannot be empty.");
    }
}
