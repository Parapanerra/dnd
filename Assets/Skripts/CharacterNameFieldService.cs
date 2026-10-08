using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Finds and synchronizes the character-name field across character-sheet scenes.
public sealed class CharacterNameFieldService
{
    private const string ObjectName = "personajName";
    private const string CurrentPath = "playerInfo/inputPises/personajName";
    private const string LegacyPath = "playerInfo/inputPises/mmpises1";

    public InputField InputField { get; private set; }
    public TMP_InputField TmpInputField { get; private set; }

    public void Cache(IEnumerable<InputField> inputs, IEnumerable<TMP_InputField> tmpInputs,
        bool allowLegacyPathFallback)
    {
        InputField = null;
        TmpInputField = null;

        foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            bool matches = allowLegacyPathFallback ? item.name == ObjectName : SceneObjectName.Matches(item.name, ObjectName);
            if (!matches)
                continue;

            InputField = item.GetComponent<InputField>();
            if (InputField == null)
                InputField = item.GetComponentInChildren<InputField>(true);
            if (InputField == null && item.parent != null)
                InputField = item.parent.GetComponent<InputField>();
            if (InputField != null)
                return;

            TmpInputField = item.GetComponent<TMP_InputField>();
            if (TmpInputField == null)
                TmpInputField = item.GetComponentInChildren<TMP_InputField>(true);
            if (TmpInputField == null && item.parent != null)
                TmpInputField = item.parent.GetComponent<TMP_InputField>();
            if (TmpInputField != null)
                return;
        }

        if (!allowLegacyPathFallback)
            return;

        InputField = FindByPath(inputs, exact: true) ?? FindByPath(inputs, exact: false);
        if (InputField != null && IsExactPath(InputField.transform))
            return;

        TMP_InputField exactTmp = FindByPath(tmpInputs, exact: true);
        if (exactTmp != null)
        {
            InputField = null;
            TmpInputField = exactTmp;
        }
        else if (InputField == null)
        {
            TmpInputField = FindByPath(tmpInputs, exact: false);
        }
    }

    public void Load(CharacterData character)
    {
        if (character == null || (InputField == null && TmpInputField == null))
            return;

        string savedName = CleanName(character.characterName);
        if (string.IsNullOrEmpty(savedName))
            return;

        if (InputField != null)
            InputField.SetTextWithoutNotify(savedName);
        else
            TmpInputField.SetTextWithoutNotify(savedName);
    }

    public void Save(CharacterData character)
    {
        if (character == null || (InputField == null && TmpInputField == null))
            return;

        string name = CleanName(InputField != null ? InputField.text : TmpInputField.text);
        if (!string.IsNullOrEmpty(name))
            character.characterName = name;
    }

    public void SubscribeIfOutsideCollectedFields(ICollection<InputField> inputs,
        ICollection<TMP_InputField> tmpInputs, Action save, Action flush)
    {
        if (InputField != null && !inputs.Contains(InputField))
        {
            InputField.onValueChanged.AddListener(delegate { save(); });
            InputField.onEndEdit.AddListener(delegate { flush(); });
        }

        if (TmpInputField != null && !tmpInputs.Contains(TmpInputField))
        {
            TmpInputField.onValueChanged.AddListener(delegate { save(); });
            TmpInputField.onEndEdit.AddListener(delegate { flush(); });
        }
    }

    public static string CleanName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        value = value.Trim();
        return float.TryParse(value, out _) ? "" : value;
    }

    private static T FindByPath<T>(IEnumerable<T> fields, bool exact) where T : Component
    {
        foreach (T field in fields)
            if (field != null && (exact ? IsExactPath(field.transform) : IsNamePath(field.transform)))
                return field;
        return null;
    }

    private static bool IsExactPath(Transform transform)
    {
        string path = GetPlainPath(transform);
        return path.EndsWith(CurrentPath, StringComparison.Ordinal) ||
               path.EndsWith(LegacyPath, StringComparison.Ordinal);
    }

    private static bool IsNamePath(Transform transform)
    {
        string path = GetPlainPath(transform);
        return path.Contains(CurrentPath) || path.Contains(LegacyPath);
    }

    private static string GetPlainPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
