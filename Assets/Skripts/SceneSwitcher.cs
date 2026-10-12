using UnityEngine;

public class SceneSwitcher : MonoBehaviour
{
    public void SwitchScene(string sceneName)
    {
        new SceneNavigator().LoadSceneByName(sceneName);
    }
}
