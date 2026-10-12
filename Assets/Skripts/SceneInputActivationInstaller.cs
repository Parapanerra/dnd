using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Wires all scene inputs; each DoubleClickInputFieldActivator handles only its own clicks.
internal sealed class SceneInputActivationInstaller
{
    internal void Install()
    {
        var configuredInputs = new List<DoubleClickInputFieldActivator>();
        foreach (InputField input in Object.FindObjectsByType<InputField>(FindObjectsInactive.Include))
            configuredInputs.Add(Configure(input));

        foreach (TMP_InputField input in Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            configuredInputs.Add(Configure(input));

        InputFieldTabNavigation navigation = Object.FindAnyObjectByType<InputFieldTabNavigation>();
        if (navigation != null)
            navigation.SetConfiguredInputs(configuredInputs);
    }

    private DoubleClickInputFieldActivator Configure(InputField input)
    {
        if (input == null)
            return null;

        DoubleClickInputFieldActivator activator = input.GetComponent<DoubleClickInputFieldActivator>();
        if (activator == null)
            activator = input.gameObject.AddComponent<DoubleClickInputFieldActivator>();

        activator.Configure(input);
        return activator;
    }

    private DoubleClickInputFieldActivator Configure(TMP_InputField input)
    {
        if (input == null)
            return null;

        DoubleClickInputFieldActivator activator = input.GetComponent<DoubleClickInputFieldActivator>();
        if (activator == null)
            activator = input.gameObject.AddComponent<DoubleClickInputFieldActivator>();

        activator.Configure(input);
        return activator;
    }
}
