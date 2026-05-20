using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownVisibilityController : MonoBehaviour
{
    public Dropdown dropdown;
    public List<GameObject> objectsToToggle;

    void Start()
    {
        // Подпишитесь на событие изменения значения в Dropdown
        if (dropdown != null)
            dropdown.onValueChanged.AddListener(delegate { RefreshVisibility(); });

        // Используем Coroutine для задержки вызова UpdateVisibility
        StartCoroutine(DelayedUpdateVisibility());
    }

    IEnumerator DelayedUpdateVisibility()
    {
        // Ждем конца текущего кадра
        yield return new WaitForEndOfFrame();
        RefreshVisibility();
    }

    public void RefreshVisibility()
    {
        if (dropdown == null || objectsToToggle == null)
            return;

        int selectedIndex = dropdown.value;

        // Если выбрано первое значение (индекс 0), скрываем все объекты
        if (selectedIndex == 0)
        {
            foreach (var obj in objectsToToggle)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
        else
        {
            // Пройдемся по всем объектам и установим их видимость в зависимости от выбранного индекса
            for (int i = 0; i < objectsToToggle.Count; i++)
            {
                if (objectsToToggle[i] == null)
                    continue;

                if (i == selectedIndex - 1)
                {
                    objectsToToggle[i].SetActive(true);
                }
                else
                {
                    objectsToToggle[i].SetActive(false);
                }
            }
        }
    }
}
