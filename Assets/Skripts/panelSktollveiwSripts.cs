using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class panelSktollveiwSripts : MonoBehaviour
{
    [System.Serializable]
    public class InventoryConfig
    {
        // public int index; // Индекс элемента в инвентаре - убираем эту переменную
        public Button mainButton; // Кнопка в основном инвентаре
        public GameObject submenuPanel; // Панель подменю (Scroll View)
        // public List<Button> submenuButtons; // Кнопки в галерее подменю - убираем эту переменную
    }

    public List<InventoryConfig> inventoryConfigs;
    public zoomCam cameraController; // Ссылка на скрипт управления камерой

    void Start()
    {
        DndSaveManager.EnsureExists();
        LoadInventoryState(); // Загрузка состояния при запуске

        foreach (var config in inventoryConfigs)
        {
            config.mainButton.onClick.AddListener(() => ToggleSubmenu(config));
            cameraController.AddScrollView(config.submenuPanel); // Добавляем каждую панель в список скролл вью
        }

        HideAllSubmenus();
    }

    private void ToggleSubmenu(InventoryConfig config)
    {
        if (config.submenuPanel.activeSelf)
        {
            config.submenuPanel.SetActive(false);
        }
        else
        {
            HideAllSubmenus();
            config.submenuPanel.SetActive(true);
            config.submenuPanel.transform.SetAsLastSibling(); // Устанавливаем подменю на верхний слой
        }
    }

    private void HideAllSubmenus()
    {
        foreach (var config in inventoryConfigs)
        {
            config.submenuPanel.SetActive(false);
        }
    }

    private void SaveInventoryState()
    {
        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();

        foreach (var config in inventoryConfigs)
        {
            string key = "SelectedImage_" + config.mainButton.name; // используем имя кнопки в качестве ключа
            Debug.Log("Deleting key: " + key);
            sceneData.DeleteString(key);
        }

        DndSaveManager.Instance.SaveData();
    }

    private void LoadInventoryState()
    {
        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();

        foreach (var config in inventoryConfigs)
        {
            string key = "SelectedImage_" + config.mainButton.name; // используем имя кнопки в качестве ключа
            if (sceneData.HasString(key))
            {
                string spriteName = sceneData.GetString(key);
                Debug.Log("Loading: " + key + " with sprite name: " + spriteName);

                // Загрузка всех спрайтов из атласа
                Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites");
                Sprite loadedSprite = System.Array.Find(sprites, sprite => sprite.name == spriteName);
                if (loadedSprite != null)
                {
                    config.mainButton.image.sprite = loadedSprite;
                }
                else
                {
                    Debug.LogWarning("Could not load sprite: " + spriteName);
                }
            }
            else
            {
                Debug.Log("No saved sprite for key: " + key);
            }
        }
    }
}
