using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class ExhaustionToggleAnimationInstaller
{
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
        Transform[] objects = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform root in objects)
        {
            SceneRoleMarker marker = root.GetComponent<SceneRoleMarker>();
            bool exhaustion = marker != null ? marker.Role == SceneRole.Exhaustion
                : SceneObjectName.Matches(root.name, "vtoma");
            if (!exhaustion)
                continue;

            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
            {
                if (toggle.GetComponent<ExhaustionToggleAnimation>() == null)
                    toggle.gameObject.AddComponent<ExhaustionToggleAnimation>();
                if (toggle.GetComponent<DropdownItemMotion>() == null)
                    toggle.gameObject.AddComponent<DropdownItemMotion>();
            }
        }
    }
}
