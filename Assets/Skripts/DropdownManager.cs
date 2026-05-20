using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownManager : MonoBehaviour
{
    [System.Serializable]
    public class DropdownConfig
    {
        public Dropdown dropdown; // Dropdown UI элемент
        public List<GameObject> tangles; // Список объектов, связанных с этим Dropdown
    }

    public List<DropdownConfig> dropdownConfigs;

    void Start()
    {
        if (dropdownConfigs == null)
            return;

        foreach (var config in dropdownConfigs)
        {
            if (config == null || config.dropdown == null)
                continue;

            // Добавляем слушатель на изменение значения в каждом Dropdown
            config.dropdown.onValueChanged.AddListener(delegate { UpdateTangles(config); });

            // Инициализируем отображение объектов при старте
            UpdateTangles(config);
        }
    }

    public void RefreshAll()
    {
        if (dropdownConfigs == null)
            return;

        foreach (var config in dropdownConfigs)
            UpdateTangles(config);
    }

    void UpdateTangles(DropdownConfig config)
    {
        if (config == null || config.dropdown == null || config.tangles == null)
            return;

        // Получаем выбранное значение из Dropdown
        int selectedNumber = config.dropdown.value;

        // Проходим по списку объектов и скрываем/показываем их
        for (int i = 0; i < config.tangles.Count; i++)
        {
            if (config.tangles[i] == null)
                continue;

            if (i < selectedNumber)
            {
                config.tangles[i].SetActive(true);
            }
            else
            {
                config.tangles[i].SetActive(false);
            }
        }
    }
}
