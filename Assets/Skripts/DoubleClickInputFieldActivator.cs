using System.Collections;
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
    private Coroutine singleClickCoroutine;

    public static void ConfigureSceneInputs()
    {
        foreach (InputField input in FindObjectsOfType<InputField>(true))
            Configure(input);

        foreach (TMP_InputField input in FindObjectsOfType<TMP_InputField>(true))
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
        input.DeactivateInputField();
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
        input.DeactivateInputField();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.unscaledTime;
        bool isDoubleClick = now - lastClickTime <= DoubleClickTime;
        lastClickTime = now;

        if (isDoubleClick)
        {
            if (singleClickCoroutine != null)
                StopCoroutine(singleClickCoroutine);

            ActivateInput();
            return;
        }

        if (singleClickCoroutine != null)
            StopCoroutine(singleClickCoroutine);

        singleClickCoroutine = StartCoroutine(DeactivateAfterSingleClick());
    }

    private IEnumerator DeactivateAfterSingleClick()
    {
        yield return null;

        if (inputField != null)
            inputField.DeactivateInputField();

        if (tmpInputField != null)
            tmpInputField.DeactivateInputField();

        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void ActivateInput()
    {
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (tmpInputField != null)
        {
            tmpInputField.Select();
            tmpInputField.ActivateInputField();
        }
    }
}
