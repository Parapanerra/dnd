using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class panelSktollveiwSripts : MonoBehaviour
{
    [System.Serializable]
    public class InventoryConfig
    {
        public Button mainButton;
        public GameObject submenuPanel;
    }

    public List<InventoryConfig> inventoryConfigs;
    public zoomCam cameraController;

    private void Start()
    {
        if (HasNewInventoryCellLayout())
        {
            enabled = false;
            return;
        }

        DndSaveManager.EnsureExists();
        LoadInventoryState();

        foreach (InventoryConfig config in inventoryConfigs)
        {
            InventoryConfig capturedConfig = config;
            config.mainButton.onClick.AddListener(() => ToggleSubmenu(capturedConfig));
            cameraController.AddScrollView(config.submenuPanel);
        }

        HideAllSubmenus();
    }

    private void ToggleSubmenu(InventoryConfig config)
    {
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
            config.submenuPanel.SetActive(false);
    }

    private bool HasNewInventoryCellLayout()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (Transform candidate in transforms)
            if (candidate != null && candidate.name == "itemCategoryDropdown")
                return true;

        return false;
    }

    private void LoadInventoryState()
    {
        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();

        foreach (InventoryConfig config in inventoryConfigs)
        {
            string key = "SelectedImage_" + config.mainButton.name;
            if (!sceneData.HasString(key))
                continue;

            string spriteName = sceneData.GetString(key);
            Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites");
            Sprite loadedSprite = System.Array.Find(sprites, sprite => sprite.name == spriteName);
            if (loadedSprite != null)
                config.mainButton.image.sprite = loadedSprite;
            else
                Debug.LogWarning("Could not load sprite: " + spriteName);
        }
    }
}
