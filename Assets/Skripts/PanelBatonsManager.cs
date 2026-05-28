using UnityEngine;
using UnityEngine.UI;

public class PanelBatonsManager : MonoBehaviour
{
    public GameObject[] panels;
    public Button[] buttons;

    private void Start()
    {
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                int index = i;
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }

        InitializePanels();
    }

    private void OnButtonClicked(int index)
    {
        if (!TryGetPanel(index, out GameObject panel))
            return;

        TogglePanel(index, !panel.activeSelf);
    }

    private void TogglePanel(int index, bool value)
    {
        if (TryGetPanel(index, out GameObject panel))
            panel.SetActive(value);
    }

    private void InitializePanels()
    {
        if (panels == null)
            return;

        for (int i = 0; i < panels.Length; i++)
            if (panels[i] != null)
                panels[i].SetActive(false);
    }

    private bool TryGetPanel(int index, out GameObject panel)
    {
        panel = null;
        if (panels == null || index < 0 || index >= panels.Length)
            return false;

        panel = panels[index];
        return panel != null;
    }
}
