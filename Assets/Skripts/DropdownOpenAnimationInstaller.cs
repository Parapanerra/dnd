using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Install a visual effect on dropdowns without changing their templates or layout.
internal static class DropdownOpenAnimationInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= Apply;
        SceneManager.sceneLoaded += Apply;
        Apply(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Apply(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
            {
                dropdown.alphaFadeSpeed = 0f;
                if (dropdown.GetComponent<DropdownWaveAnimator>() == null)
                    dropdown.gameObject.AddComponent<DropdownWaveAnimator>();
            }

            foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                dropdown.alphaFadeSpeed = 0f;
                if (dropdown.GetComponent<DropdownWaveAnimator>() == null)
                    dropdown.gameObject.AddComponent<DropdownWaveAnimator>();
            }
        }
    }
}
