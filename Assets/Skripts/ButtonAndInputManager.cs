using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAndInputManager : MonoBehaviour
{
    public List<Button> buttons;
    public List<InputField> inputFields;

    private void Start()
    {
        DisableOldOverlayButtons();
        ConfigureInputFields();
    }

    private void DisableOldOverlayButtons()
    {
        foreach (Button button in buttons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
    }

    private void ConfigureInputFields()
    {
        foreach (InputField inputField in inputFields)
            DoubleClickInputFieldActivator.Configure(inputField);

        DoubleClickInputFieldActivator.ConfigureSceneInputs();
    }
}
