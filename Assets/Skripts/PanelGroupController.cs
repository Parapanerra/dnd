using UnityEngine;
using UnityEngine.UI;

// One panel group with the interaction selected in the Inspector.
public sealed class PanelGroupController : MonoBehaviour
{
    private enum InteractionMode
    {
        IndependentButtons = 0,
        ToggleControlled = 1,
        ExclusiveButtons = 2
    }

    [SerializeField] private InteractionMode mode;
    [SerializeField] private GameObject[] panels;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Toggle[] toggles;

    private int activePanelIndex = -1;

    private void Start()
    {
        if (mode == InteractionMode.ToggleControlled)
        {
            BindToggles();
            return;
        }

        BindButtons();
        HideAllPanels();
        if (mode == InteractionMode.ExclusiveButtons && panels != null && panels.Length > 0)
        {
            activePanelIndex = 0;
            SetPanelActive(activePanelIndex, true);
        }
    }

    private void BindButtons()
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            int index = i;
            buttons[i].onClick.AddListener(() => OnButtonClicked(index));
        }
    }

    private void BindToggles()
    {
        if (toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] == null)
                continue;

            int index = i;
            toggles[i].onValueChanged.AddListener(value => SetPanelActive(index, value));
            SetPanelActive(index, toggles[i].isOn);
        }
    }

    private void OnButtonClicked(int index)
    {
        if (!TryGetPanel(index, out GameObject panel))
            return;

        if (mode == InteractionMode.ExclusiveButtons)
        {
            if (activePanelIndex == index)
                return;

            HideAllPanels();
            SetPanelActive(index, true);
            activePanelIndex = index;
        }
        else
        {
            SetPanelActive(index, !panel.activeSelf);
        }
    }

    // Reapply toggle state after loading a character scene.
    public void RefreshPanels()
    {
        if (mode != InteractionMode.ToggleControlled || toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
            if (toggles[i] != null)
                SetPanelActive(i, toggles[i].isOn);
    }

    private void HideAllPanels()
    {
        if (panels == null)
            return;

        foreach (GameObject panel in panels)
            if (panel != null)
                panel.SetActive(false);
    }

    private void SetPanelActive(int index, bool active)
    {
        if (TryGetPanel(index, out GameObject panel))
            panel.SetActive(active);
    }

    private bool TryGetPanel(int index, out GameObject panel)
    {
        panel = panels != null && index >= 0 && index < panels.Length ? panels[index] : null;
        return panel != null;
    }
}
