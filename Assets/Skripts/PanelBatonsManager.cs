using UnityEngine;
using UnityEngine.UI;

public class PanelBatonsManager : MonoBehaviour
{
    public GameObject[] panels;
    public Button[] buttons;

    void Start()
    {
        // Настроим обработчики событий для каждой кнопки
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // Захватываем текущее значение i для использования внутри замыкания
            buttons[i].onClick.AddListener(() => OnButtonClicked(index));
        }

        // Изначально все панели выключены
        InitializePanels();
    }

    // Метод для обработки нажатия на кнопку
    void OnButtonClicked(int index)
    {
        // Переключаем состояние панели
        bool isPanelActive = !panels[index].activeSelf;
        TogglePanel(index, isPanelActive);
    }

    // Метод для скрытия или отображения панели
    void TogglePanel(int index, bool value)
    {
        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(value);
        }
    }

    // Метод для инициализации панелей (выключение всех панелей)
    void InitializePanels()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(false);
        }
    }
}
