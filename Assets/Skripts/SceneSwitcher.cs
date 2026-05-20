using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    private string previousScene;

    public void SwitchScene(string sceneName)
    {
        previousScene = SceneManager.GetActiveScene().name;
        load_scenes.LoadSceneByName(sceneName);
    }

    public void GoBack()
    {
        if (!string.IsNullOrEmpty(previousScene))
        {
            SceneManager.LoadScene(previousScene);
        }
        else
        {
            Debug.LogError("Previous scene name is not set.");
        }
    }
}
