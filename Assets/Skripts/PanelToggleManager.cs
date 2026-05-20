using UnityEngine;
using UnityEngine.UI;

public class PanelToggleManager : MonoBehaviour
{
    public GameObject[] panels;
    public Toggle[] toggles;

    void Start()
    {
        // Настраиваем обработчики событий для каждого переключателя (Toggle)
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i; // Захватываем текущее значение i для использования внутри замыкания
            toggles[i].onValueChanged.AddListener((value) => OnToggleChanged(index, value));

            // Изначально отображаем или скрываем панель в зависимости от состояния переключателя
            panels[index].SetActive(toggles[index].isOn);
        }
    }

    // Метод для обработки изменения состояния переключателя
    void OnToggleChanged(int index, bool isOn)
    {
        // Переключаем состояние панели
        TogglePanel(index, isOn);
    }

    // Метод для скрытия или отображения панели
    void TogglePanel(int index, bool value)
    {
        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(value);
        }
    }
}
