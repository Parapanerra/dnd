using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Coordinates the UI owned by the active character scene controller.
internal sealed class CharacterSceneUiService
{
    internal void RefreshAfterLoad()
    {
        RefreshDropdowns();
        RefreshPanels();

        foreach (HealthBar healthBar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (healthBar != null)
                healthBar.RefreshHealthFromData();
    }

    internal void RefreshAfterReset()
    {
        RefreshDropdowns();
        RefreshPanels();
    }

    internal void ResetSceneWidgets()
    {
        foreach (InventoryItemCell cell in Object.FindObjectsByType<InventoryItemCell>(FindObjectsInactive.Include))
            if (cell != null)
                cell.ResetToDefaults(false);

        foreach (HealthBar healthBar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (healthBar != null)
                healthBar.ResetHealth();
    }

    internal CharacterPortraitManager EnsurePortraitManager(GameObject host, DndSaveManager saveManager)
    {
        if (host == null)
            return null;

        CharacterPortraitManager existing = Object.FindAnyObjectByType<CharacterPortraitManager>();
        if (existing != null)
        {
            existing.Initialize(saveManager);
            return existing;
        }

        bool hasPortraitUi = false;
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (transform != null && (SceneObjectName.Matches(transform.name, "Buttonphotopersoj") ||
                                      SceneObjectName.Matches(transform.name, "photopersonaja")))
            {
                hasPortraitUi = true;
                break;
            }

        if (!hasPortraitUi)
            return null;

        CharacterPortraitManager created = host.AddComponent<CharacterPortraitManager>();
        created.Initialize(saveManager);
        return created;
    }

    internal bool IsResetButton(Button button)
    {
        if (button == null)
            return false;

        SceneRoleMarker marker = button.GetComponent<SceneRoleMarker>();
        if (marker != null)
            return marker.Role == SceneRole.ResetScene;

        string name = button.gameObject.name.ToLowerInvariant();
        return name.Contains("resetseve") || name.Contains("reset save") ||
               name.Contains("resetsave") || name.Contains("clear save");
    }

    internal void DisablePersistentOnClick(Button button)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private void RefreshDropdowns()
    {
        foreach (DropdownManager manager in Object.FindObjectsByType<DropdownManager>(FindObjectsInactive.Include))
            if (manager != null)
                manager.RefreshAll();

        foreach (DropdownVisibilityController controller in Object.FindObjectsByType<DropdownVisibilityController>(FindObjectsInactive.Include))
            if (controller != null)
                controller.RefreshVisibility();
    }

    private void RefreshPanels()
    {
        foreach (PanelGroupController manager in Object.FindObjectsByType<PanelGroupController>(FindObjectsInactive.Include))
            if (manager != null)
                manager.RefreshPanels();
    }
}
