using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LinkHandler : MonoBehaviour
{
    public List<Button> linkButtons; // Список кнопок
    public List<string> urls; // Список URL-адрес

    void Start()
    {
        // Перевірка, чи всі елементи присутні у списках
        for (int i = 0; i < linkButtons.Count; i++)
        {
            if (i < urls.Count)
            {
                int index = i; // Локальна копія для замикання
                linkButtons[i].onClick.AddListener(() => OpenLink(urls[index]));
            }
            else
            {
                Debug.LogError("URL list is shorter than button list!");
            }
        }
    }

    void OpenLink(string url)
    {
        Application.OpenURL(url); // Відкриття посилання в браузері
    }
}
