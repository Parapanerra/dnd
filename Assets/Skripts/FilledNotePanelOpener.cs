using System;
using UnityEngine;
using UnityEngine.UI;

// Open newly populated notes after scene fields have loaded. A manual close stays closed
// until the note text changes, so this never fights the player's toggle while reading.
[DefaultExecutionOrder(300)]
public sealed class FilledNotePanelOpener : MonoBehaviour
{
    [SerializeField] private InputField note;
    [SerializeField] private Toggle toggle;
    [SerializeField] private InputField[] additionalFields = Array.Empty<InputField>();
    [SerializeField] private bool openWhenLoadedWithText;
    private DndSaveManager save;
    private PersistentFieldId identity;

    private void Start()
    {
        if (note == null || toggle == null) return;
        identity = PersistentFieldId.For(note);
        save = new RuntimeManagerResolver().SaveManager();
        note.onEndEdit.AddListener(OnEdited);
        foreach (var field in additionalFields)
            if (field != null) field.onEndEdit.AddListener(OnEdited);
        OpenIfChanged();
    }
    private void OnDestroy()
    {
        if (note != null) note.onEndEdit.RemoveListener(OnEdited);
        foreach (var field in additionalFields)
            if (field != null) field.onEndEdit.RemoveListener(OnEdited);
    }
    private void OnEdited(string value) => OpenIfChanged();
    private void OpenIfChanged()
    {
        if (openWhenLoadedWithText)
        {
            if (!string.IsNullOrWhiteSpace(note.text) || Array.Exists(additionalFields,
                    field => field != null && !string.IsNullOrWhiteSpace(field.text))) toggle.isOn = true;
            return;
        }
        if (identity == null || save == null || !save.IsInitialized) return;
        var character = save.EnsureActiveCharacter();
        if (character == null) return;
        var data = character.GetSceneData(save.GetActiveSceneDataName());
        if (Apply(data, identity.Id, note.text, () => toggle.isOn = true)) save.RequestSaveData();
    }
    internal static bool Apply(CharacterSceneData data, string fieldId, string text, Action open)
    {
        string key = "NoteOpenedForText_v1_" + fieldId;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (!data.HasString(key)) return false;
            data.DeleteString(key);
            return true;
        }
        // Stable, compact content fingerprint; the note itself is saved only by the field system.
        ulong hash = 14695981039346656037UL;
        unchecked { foreach (char value in text) { hash ^= value; hash *= 1099511628211UL; } }
        string stamp = hash.ToString("X16");
        if (data.GetString(key, "") == stamp) return false;
        open();
        data.SetString(key, stamp);
        return true;
    }
}
