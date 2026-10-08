using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Refreshes and resets scene UI that is shared by both character-sheet adapters.
public static class CharacterSceneUiService
{
    public static void RefreshAfterLoad()
    {
        RefreshDropdowns();
        RefreshPanels();

        foreach (HealthBar healthBar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (healthBar != null)
                healthBar.RefreshHealthFromData();
    }

    public static void RefreshAfterReset()
    {
        RefreshDropdowns();
        RefreshPanels();
    }

    public static void ResetSceneWidgets()
    {
        foreach (InventoryItemCell cell in Object.FindObjectsByType<InventoryItemCell>(FindObjectsInactive.Include))
            if (cell != null)
                cell.ResetToDefaults(false);

        foreach (HealthBar healthBar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (healthBar != null)
                healthBar.ResetHealth();
    }

    public static void EnsurePortraitManager(GameObject host)
    {
        if (host == null || Object.FindAnyObjectByType<CharacterPortraitManager>() != null)
            return;

        bool hasPortraitUi = false;
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (transform != null && (SceneObjectName.Matches(transform.name, "Buttonphotopersoj") ||
                                      SceneObjectName.Matches(transform.name, "photopersonaja")))
            {
                hasPortraitUi = true;
                break;
            }

        if (hasPortraitUi)
            host.AddComponent<CharacterPortraitManager>();
    }

    public static bool IsResetButton(Button button)
    {
        if (button == null)
            return false;

        SceneRoleMarker marker = button.GetComponent<SceneRoleMarker>();
        if (marker != null)
            return marker.role == SceneRole.ResetScene;

        string name = button.gameObject.name.ToLowerInvariant();
        return name.Contains("resetseve") || name.Contains("reset save") ||
               name.Contains("resetsave") || name.Contains("clear save");
    }

    public static void DisablePersistentOnClick(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private static void RefreshDropdowns()
    {
        foreach (DropdownManager manager in Object.FindObjectsByType<DropdownManager>(FindObjectsInactive.Include))
            if (manager != null)
                manager.RefreshAll();

        foreach (DropdownVisibilityController controller in Object.FindObjectsByType<DropdownVisibilityController>(FindObjectsInactive.Include))
            if (controller != null)
                controller.RefreshVisibility();
    }

    private static void RefreshPanels()
    {
        foreach (PanelToggleManager manager in Object.FindObjectsByType<PanelToggleManager>(FindObjectsInactive.Include))
            if (manager != null)
                manager.RefreshPanels();
    }
}
