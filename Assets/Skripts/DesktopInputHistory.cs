using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Unity's input fields handle selection, clipboard, and caret movement themselves.
// This shared adapter supplies only the missing desktop undo/redo history.
public sealed class DesktopInputHistory : MonoBehaviour, ISelectHandler
{
    private const int MaxEntries = 100;
    private readonly List<string> entries = new List<string>();
    private InputField legacyInput;
    private TMP_InputField tmpInput;
    private int currentEntry;
    private bool restoring;
    private int pendingLinePosition = -1;
    private int pendingSelectionAnchor;
    private bool pendingShift;

    internal void Configure(InputField input)
    {
        Unsubscribe();
        legacyInput = input;
        tmpInput = null;
        legacyInput.onValueChanged.AddListener(RecordChange);
        ResetHistory();
    }

    internal void Configure(TMP_InputField input)
    {
        Unsubscribe();
        legacyInput = null;
        tmpInput = input;
        tmpInput.onValueChanged.AddListener(RecordChange);
        ResetHistory();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Unsubscribe()
    {
        if (legacyInput != null)
            legacyInput.onValueChanged.RemoveListener(RecordChange);
        if (tmpInput != null)
            tmpInput.onValueChanged.RemoveListener(RecordChange);
    }

    private string CurrentText => legacyInput != null ? legacyInput.text : tmpInput != null ? tmpInput.text : "";

    private void ResetHistory()
    {
        entries.Clear();
        entries.Add(CurrentText);
        currentEntry = 0;
    }

    public void OnSelect(BaseEventData eventData)
    {
        // Loading a character uses SetTextWithoutNotify; start a new history for that value.
        if (entries.Count == 0 || entries[currentEntry] != CurrentText)
            ResetHistory();
    }

    private void RecordChange(string value)
    {
        if (restoring)
            return;

        if (entries.Count == 0)
            ResetHistory();

        if (entries[currentEntry] == value)
            return;

        if (currentEntry < entries.Count - 1)
            entries.RemoveRange(currentEntry + 1, entries.Count - currentEntry - 1);

        entries.Add(value);
        currentEntry = entries.Count - 1;
        if (entries.Count > MaxEntries)
        {
            entries.RemoveAt(0);
            currentEntry--;
        }
    }

    private void Update()
    {
        if (Application.isMobilePlatform || !Application.isFocused || EventSystem.current == null ||
            EventSystem.current.currentSelectedGameObject != gameObject ||
            (legacyInput != null && !legacyInput.isFocused) ||
            (tmpInput != null && !tmpInput.isFocused))
            return;

        bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (legacyInput != null && legacyInput.lineType != InputField.LineType.SingleLine && !control && !alt)
        {
            int caret = legacyInput.caretPosition;
            if (Input.GetKeyDown(KeyCode.Home))
                pendingLinePosition = legacyInput.text.LastIndexOf('\n', Mathf.Max(0, caret - 1)) + 1;
            else if (Input.GetKeyDown(KeyCode.End))
            {
                int nextBreak = legacyInput.text.IndexOf('\n', caret);
                pendingLinePosition = nextBreak < 0 ? legacyInput.text.Length : nextBreak;
            }

            if (pendingLinePosition >= 0)
            {
                pendingSelectionAnchor = legacyInput.selectionAnchorPosition;
                pendingShift = shift;
            }
        }

        if (!control || alt)
            return;

        if (Input.GetKeyDown(KeyCode.Z))
            Restore(shift ? currentEntry + 1 : currentEntry - 1);
        else if (Input.GetKeyDown(KeyCode.Y))
            Restore(currentEntry + 1);
    }

    private void LateUpdate()
    {
        if (pendingLinePosition < 0 || legacyInput == null || !legacyInput.isFocused)
        {
            pendingLinePosition = -1;
            return;
        }

        if (pendingShift)
        {
            legacyInput.selectionAnchorPosition = pendingSelectionAnchor;
            legacyInput.selectionFocusPosition = pendingLinePosition;
        }
        else
            legacyInput.caretPosition = pendingLinePosition;

        legacyInput.ForceLabelUpdate();
        pendingLinePosition = -1;
    }

    private void Restore(int index)
    {
        if (index < 0 || index >= entries.Count)
            return;

        // A silent assignment would bypass the existing onValueChanged autosave listeners.
        restoring = true;
        currentEntry = index;
        if (legacyInput != null)
        {
            legacyInput.text = entries[index];
            legacyInput.caretPosition = legacyInput.text.Length;
        }
        else if (tmpInput != null)
        {
            tmpInput.text = entries[index];
            tmpInput.caretPosition = tmpInput.text.Length;
        }
        restoring = false;
    }
}
