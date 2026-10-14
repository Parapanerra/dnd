using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySubmenuController : MonoBehaviour
{
    [System.Serializable]
    private class InventoryConfig
    {
        public Button mainButton;
        public GameObject submenuPanel;
    }

    [SerializeField] private List<InventoryConfig> inventoryConfigs;
    [SerializeField] private MapCameraController cameraController;
    private DndSaveManager saveManager;

    internal void Initialize(DndSaveManager manager)
    {
        saveManager = manager;
    }

    private void Start()
    {
        if (inventoryConfigs == null || inventoryConfigs.Count == 0 || HasNewInventoryCellLayout())
        {
            enabled = false;
            return;
        }

        if (saveManager == null)
        {
            Debug.LogError("Inventory scroll panel was not initialized by the character scene controller.");
            return;
        }

        LoadInventoryState();

        foreach (InventoryConfig config in inventoryConfigs)
        {
            if (config == null || config.mainButton == null || config.submenuPanel == null)
                continue;

            InventoryConfig capturedConfig = config;
            config.mainButton.onClick.AddListener(() => ToggleSubmenu(capturedConfig));
            if (cameraController != null)
                cameraController.AddScrollView(config.submenuPanel);
        }

        HideAllSubmenus();
    }

    private void OnDestroy()
    {
        if (cameraController == null || inventoryConfigs == null)
            return;

        foreach (InventoryConfig config in inventoryConfigs)
            if (config != null && config.submenuPanel != null)
                cameraController.RemoveScrollView(config.submenuPanel);
    }

    private void ToggleSubmenu(InventoryConfig config)
    {
        if (config == null || config.submenuPanel == null)
            return;

        if (config.submenuPanel.activeSelf)
        {
            config.submenuPanel.SetActive(false);
            return;
        }

        HideAllSubmenus();
        config.submenuPanel.SetActive(true);
        config.submenuPanel.transform.SetAsLastSibling();
    }

    private void HideAllSubmenus()
    {
        foreach (InventoryConfig config in inventoryConfigs)
            if (config != null && config.submenuPanel != null)
                config.submenuPanel.SetActive(false);
    }

    private bool HasNewInventoryCellLayout()
    {
        foreach (UiControlMarker marker in FindObjectsByType<UiControlMarker>(FindObjectsInactive.Include))
            if (marker.gameObject.scene == gameObject.scene && marker.Role == UiControlRole.ItemCategory)
                return true;

        // Compatibility for scenes that do not yet have typed controls.
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
            if (candidate != null && candidate.name == "itemCategoryDropdown")
                return true;

        return false;
    }

    private void LoadInventoryState()
    {
        CharacterSceneData sceneData = saveManager.GetActiveSceneData();
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites");

        foreach (InventoryConfig config in inventoryConfigs)
        {
            if (config == null || config.mainButton == null)
                continue;

            string key = "SelectedImage_" + config.mainButton.name;
            if (!sceneData.HasString(key))
                continue;

            string spriteName = sceneData.GetString(key);
            Sprite loadedSprite = System.Array.Find(sprites, sprite => sprite.name == spriteName);
            if (loadedSprite != null)
                config.mainButton.image.sprite = loadedSprite;
            else
                Debug.LogWarning("Could not load sprite: " + spriteName);
        }
    }
}
