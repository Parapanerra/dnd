using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class RoundToggleAnimationInstaller
{
    // The circular frame shared by skill, save, spell, resource, and death-save toggles.
    private const string RoundFrameSprite = "_59b7a82e-e760-4963-9141-913d1f8420c92";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Install();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Install();
    }

    private static void Install()
    {
        Toggle[] toggles = Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Toggle toggle in toggles)
        {
            Image frame = toggle.targetGraphic as Image;
            if (frame == null || frame.sprite == null || frame.sprite.name != RoundFrameSprite || toggle.graphic == null)
                continue;

            if (toggle.GetComponent<RoundToggleAnimation>() == null)
                toggle.gameObject.AddComponent<RoundToggleAnimation>();
        }
    }
}
