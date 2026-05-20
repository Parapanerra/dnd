using UnityEngine;
using UnityEngine.UI;

public class InputFieldIncrementer : MonoBehaviour
{
    [System.Serializable]
    public class FieldConfig
    {
        public InputField inputField; // Поле ввода
        public Button incrementButton; // Кнопка увеличения
        public Button decrementButton; // Кнопка уменьшения
        public CharacterSheetManagerScene1 characterSheetManager; // Ссылка на CharacterSheetManagerScene1 для сохранения данных
    }

    public FieldConfig[] fieldConfigs; // Массив конфигураций для каждого поля ввода

    void Start()
    {
        foreach (var config in fieldConfigs)
        {
            config.incrementButton.onClick.AddListener(() => IncrementValue(config));
            config.decrementButton.onClick.AddListener(() => DecrementValue(config));
        }
    }

    private void IncrementValue(FieldConfig config)
    {
        if (int.TryParse(config.inputField.text, out int value))
        {
            value += 1;
            config.inputField.text = value.ToString();
            config.characterSheetManager.SaveCharacterData(); // Сохраняем данные после изменения
        }
    }

    private void DecrementValue(FieldConfig config)
    {
        if (int.TryParse(config.inputField.text, out int value))
        {
            value -= 1;
            config.inputField.text = value.ToString();
            config.characterSheetManager.SaveCharacterData(); // Сохраняем данные после изменения
        }
    }
}
