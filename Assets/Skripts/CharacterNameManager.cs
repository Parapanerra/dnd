using UnityEngine;
using UnityEngine.UI;

public class CharacterNameManager : MonoBehaviour
{
    // Цей скрипт раніше керував іменами в меню, але тепер ми використовуємо MainMenuManager.
    // Поля залишені, щоб не злетіли посилання (Missing Reference) в Unity, якщо цей скрипт ще десь висить.
    public InputField[] nameInputFields;
    public Button[] confirmButtons;
    public Button[] resetButtons;
    public Button[] characterButtons;

    public void UpdateAllNameButtonsUI()
    {
        // Логіка перенесена в MainMenuManager.cs
    }
}
