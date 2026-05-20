using UnityEngine;
using UnityEngine.UI;

public class InformationButtonsManager : MonoBehaviour
{
    public GameObject[] panels;
    public Button[] buttons;

    private int activePanelIndex = -1; // Индекс текущей активной панели

    private void Start()
    {
        // Настраиваем обработчики событий для каждой кнопки
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i; // Захватываем текущее значение i для использования внутри замыкания
            buttons[i].onClick.AddListener(() => OnButtonClick(index));
        }

        // Устанавливаем первую панель как активную при старте
        activePanelIndex = 0;
        ActivateInitialPanel();
    }

    // Метод для обработки нажатия кнопки
    void OnButtonClick(int index)
    {
        // Если уже активная панель была выбрана снова, не делаем ничего
        if (activePanelIndex == index)
        {
            return;
        }

        // Закрываем все панели
        DeactivateAllPanels();

        // Открываем панель, соответствующую нажатой кнопке
        TogglePanel(index, true);

        // Обновляем индекс активной панели
        activePanelIndex = index;
    }

    // Метод для скрытия или отображения панели
    void TogglePanel(int index, bool value)
    {
        if (index >= 0 && index < panels.Length)
        {
            panels[index].SetActive(value);
        }
    }

    // Метод для деактивации всех панелей
    void DeactivateAllPanels()
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }

    // Метод для активации начальной панели
    void ActivateInitialPanel()
    {
        DeactivateAllPanels(); // Выключаем все панели
        TogglePanel(activePanelIndex, true); // Включаем первую панель
    }
}
