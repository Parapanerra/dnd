using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoubleClickInputFieldActivator : MonoBehaviour, IPointerClickHandler
{
    private const float DoubleClickTime = 0.35f;

    private InputField inputField;
    private TMP_InputField tmpInputField;
    private float lastClickTime = -1f;

    public static void ConfigureSceneInputs()
    {
        foreach (InputField input in FindObjectsByType<InputField>(FindObjectsInactive.Include))
            Configure(input);

        foreach (TMP_InputField input in FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include))
            Configure(input);
    }

    public static void Configure(InputField input)
    {
        if (input == null)
            return;

        DoubleClickInputFieldActivator activator = input.GetComponent<DoubleClickInputFieldActivator>();
        if (activator == null)
            activator = input.gameObject.AddComponent<DoubleClickInputFieldActivator>();

        activator.inputField = input;
        activator.tmpInputField = null;
        input.onEndEdit.RemoveListener(activator.LockLegacyInput);
        input.onEndEdit.AddListener(activator.LockLegacyInput);
        activator.LockInput();
    }

    public static void Configure(TMP_InputField input)
    {
        if (input == null)
            return;

        DoubleClickInputFieldActivator activator = input.GetComponent<DoubleClickInputFieldActivator>();
        if (activator == null)
            activator = input.gameObject.AddComponent<DoubleClickInputFieldActivator>();

        activator.inputField = null;
        activator.tmpInputField = input;
        input.onEndEdit.RemoveListener(activator.LockTmpInput);
        input.onEndEdit.AddListener(activator.LockTmpInput);
        activator.LockInput();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.unscaledTime;
        bool isDoubleClick = now - lastClickTime <= DoubleClickTime;
        lastClickTime = now;

        if (isDoubleClick)
        {
            ActivateInput();
            return;
        }

        LockInput();
    }

    private void LockInput()
    {
        if (inputField != null)
        {
            inputField.DeactivateInputField();
            inputField.enabled = false;
        }

        if (tmpInputField != null)
        {
            tmpInputField.DeactivateInputField();
            tmpInputField.enabled = false;
        }

        // Do not call EventSystem.SetSelectedGameObject here: LockInput can run from
        // InputField.onEndEdit while EventSystem is already changing selection.
    }

    private void LockLegacyInput(string value)
    {
        LockInput();
    }

    private void LockTmpInput(string value)
    {
        LockInput();
    }

    private void ActivateInput()
    {
        if (inputField != null)
        {
            inputField.enabled = true;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (tmpInputField != null)
        {
            tmpInputField.enabled = true;
            tmpInputField.Select();
            tmpInputField.ActivateInputField();
        }
    }
}
