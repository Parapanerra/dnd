using UnityEngine;
using UnityEngine.UI;

public class PanelToggleManager : MonoBehaviour
{
    public GameObject[] panels;
    public Toggle[] toggles;

    private void Start()
    {
        if (toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == null)
                continue;

            int index = i;
            toggles[i].onValueChanged.AddListener(value => OnToggleChanged(index, value));
            RefreshPanel(index);
        }
    }

    public void RefreshPanels()
    {
        if (toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
            RefreshPanel(i);
    }

    private void OnToggleChanged(int index, bool isOn)
    {
        TogglePanel(index, isOn);
    }

    private void TogglePanel(int index, bool value)
    {
        if (panels == null || index < 0 || index >= panels.Length)
            return;

        if (panels[index] != null)
            panels[index].SetActive(value);
    }

    private void RefreshPanel(int index)
    {
        if (panels == null || toggles == null || index < 0 || index >= panels.Length || index >= toggles.Length)
            return;

        if (panels[index] != null && toggles[index] != null)
            panels[index].SetActive(toggles[index].isOn);
    }
}
