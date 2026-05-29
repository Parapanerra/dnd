using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryWithSubmenus : MonoBehaviour
{
    [System.Serializable]
    public class InventoryConfig
    {
        public int index; // Индекс элемента в инвентаре
        public Button mainButton; // Кнопка в основном инвентаре
        public GameObject submenuPanel; // Панель подменю (Scroll View)
        public List<Image> submenuImages; // Изображения в галерее подменю
        public Sprite originalSprite; // Изначальная картинка для основной кнопки
        public bool resetApplied; // Переменная для отслеживания применения сброса
    }

    public List<InventoryConfig> inventoryConfigs;
    public Button resetButton; // Кнопка сброса

    void Start()
    {
        if (HasNewInventoryCellLayout())
        {
            enabled = false;
            return;
        }

        DndSaveManager.EnsureExists();

        // Проверка уникальности индексов
        HashSet<int> uniqueIndexes = new HashSet<int>();
        foreach (var config in inventoryConfigs)
        {
            if (!uniqueIndexes.Add(config.index))
            {
                Debug.LogError("Индексы элементов инвентаря должны быть уникальными. Дублирующийся индекс: " + config.index);
            }
        }

        LoadInventoryState(); // Загрузка состояния при запуске

        foreach (var config in inventoryConfigs)
        {
            config.mainButton.onClick.AddListener(() => ToggleSubmenu(config));

            foreach (var image in config.submenuImages)
            {
                int configIndex = config.index; // Используем локальную переменную для захвата правильного индекса
                image.GetComponent<Button>().onClick.AddListener(() => OnSubmenuImageClicked(image, configIndex));
            }
        }

        HideAllSubmenus();

        if (resetButton != null)
        {
            resetButton.onClick.AddListener(() => {
                ResetInventory();
                SaveInventoryState();
                LoadInventoryState(); // Мгновенно обновляем UI после сброса
            });
        }
    }

    private bool HasNewInventoryCellLayout()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
            if (transform != null && transform.name == "itemCategoryDropdown")
                return true;

        return false;
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

    private void OnSubmenuImageClicked(Image submenuImage, int configIndex)
    {
        // Проверяем, есть ли активная кнопка основного меню
        if (configIndex >= 0 && configIndex < inventoryConfigs.Count)
        {
            InventoryConfig config = inventoryConfigs.Find(c => c.index == configIndex);
            if (config != null)
            {
                // Обновляем изображение на кнопке в основном инвентаре
                config.mainButton.image.sprite = submenuImage.sprite;
                config.mainButton.image.color = submenuImage.color; // Обновляем цвет, если необходимо
                SaveInventoryState(); // Сохраняем состояние после выбора изображения

                // Закрываем все подменю после выбора
                HideAllSubmenus();

                // Устанавливаем флаг сброса в false после выбора новой картинки
                config.resetApplied = false;
            }
        }
    }

    private void ResetInventory()
    {
        foreach (var config in inventoryConfigs)
        {
            // Применяем originalSprite только если сброс не был применен ранее
            if (!config.resetApplied)
            {
                config.mainButton.image.sprite = config.originalSprite;
            }
        }

        // Устанавливаем флаг сброса в true после нажатия на кнопку сброса
        foreach (var config in inventoryConfigs)
        {
            config.resetApplied = true;
        }

        SaveInventoryState(); // Сохраняем состояние после сброса
    }

    private void SaveInventoryState()
    {
        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();

        foreach (var config in inventoryConfigs)
        {
            string key = "SelectedImage_" + config.index;
            if (config.mainButton.image.sprite != config.originalSprite)
            {
                Debug.Log("Saving: " + key + " with sprite name: " + config.mainButton.image.sprite.name);
                sceneData.SetString(key, config.mainButton.image.sprite.name);
            }
            else
            {
                Debug.Log("Deleting key: " + key);
                sceneData.DeleteString(key);
            }
        }

        DndSaveManager.Instance.SaveData();
    }

    private void LoadInventoryState()
    {
        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();

        foreach (var config in inventoryConfigs)
        {
            string key = "SelectedImage_" + config.index;
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
