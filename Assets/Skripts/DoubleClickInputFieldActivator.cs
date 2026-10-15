using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DoubleClickInputFieldActivator : MonoBehaviour, IPointerClickHandler
{
    private InputField inputField;
    private TMP_InputField tmpInputField;
    private float lastClickTime = -1f;

    internal void Configure(InputField input)
    {
        inputField = input;
        tmpInputField = null;
        input.onEndEdit.RemoveListener(LockLegacyInput);
        input.onEndEdit.AddListener(LockLegacyInput);
        if (!Application.isMobilePlatform)
        {
            input.customCaretColor = true;
            input.caretColor = new Color32(255, 184, 66, 255);
            input.caretWidth = 3;
            input.caretBlinkRate = 0.85f;
        }
        LockInput();
    }

    internal void Configure(TMP_InputField input)
    {
        inputField = null;
        tmpInputField = input;
        input.onEndEdit.RemoveListener(LockTmpInput);
        input.onEndEdit.AddListener(LockTmpInput);
        if (!Application.isMobilePlatform)
        {
            input.customCaretColor = true;
            input.caretColor = new Color32(255, 184, 66, 255);
            input.caretWidth = 3;
            input.caretBlinkRate = 0.85f;
        }
        LockInput();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Once a double click has unlocked the field, ordinary clicks and drags
        // should move or select its text instead of locking it again.
        if ((inputField != null && inputField.enabled) ||
            (tmpInputField != null && tmpInputField.enabled))
        {
            lastClickTime = -1f;
            return;
        }

        float now = Time.unscaledTime;
        bool isDoubleClick = now - lastClickTime <= AppConfig.Input.DoubleClickTimeSeconds;
        lastClickTime = now;

        if (isDoubleClick)
        {
            ActivateInput();
            return;
        }

        LockInput();
    }

    internal bool TryGetInput(out Selectable selectable, out RectTransform rectTransform)
    {
        if (inputField != null)
            selectable = inputField;
        else
            selectable = tmpInputField;

        rectTransform = selectable != null ? selectable.transform as RectTransform : null;
        return selectable != null && rectTransform != null;
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

    internal void ActivateInput()
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
