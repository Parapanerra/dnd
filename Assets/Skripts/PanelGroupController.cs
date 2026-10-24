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
            SetPanelActive(activePanelIndex, true, false);
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
            toggles[i].onValueChanged.AddListener(value => SetPanelActive(index, value, true));
            SetPanelActive(index, toggles[i].isOn, false);
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

            HideAllPanels(true);
            SetPanelActive(index, true, true);
            activePanelIndex = index;
        }
        else
        {
            PanelVisibilityAnimation animation = panel.GetComponent<PanelVisibilityAnimation>();
            SetPanelActive(index, !(animation != null ? animation.IsOpen : panel.activeSelf), true);
        }
    }

    // Reapply toggle state after loading a character scene.
    public void RefreshPanels()
    {
        if (mode != InteractionMode.ToggleControlled || toggles == null)
            return;

        for (int i = 0; i < toggles.Length; i++)
            if (toggles[i] != null)
                SetPanelActive(i, toggles[i].isOn, false);
    }

    private void HideAllPanels(bool animate = false)
    {
        if (panels == null)
            return;

        foreach (GameObject panel in panels)
            if (panel != null)
                SetPanelVisible(panel, false, animate);
    }

    private void SetPanelActive(int index, bool active, bool animate)
    {
        if (TryGetPanel(index, out GameObject panel))
            SetPanelVisible(panel, active, animate);
    }

    private void SetPanelVisible(GameObject panel, bool visible, bool animate)
    {
        PanelVisibilityAnimation animation = panel.GetComponent<PanelVisibilityAnimation>();
        if (animation == null)
            animation = panel.AddComponent<PanelVisibilityAnimation>();
        animation.SetVisible(visible, animate);
    }

    private bool TryGetPanel(int index, out GameObject panel)
    {
        panel = panels != null && index >= 0 && index < panels.Length ? panels[index] : null;
        return panel != null;
    }
}
