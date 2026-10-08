using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reads and writes the two character-wide input fields shared by multiple scenes.
public static class SharedCharacterInputService
{
    public static void Save(CharacterData character, IEnumerable<InputField> inputs,
        IEnumerable<TMP_InputField> tmpInputs)
    {
        if (character == null)
            return;

        foreach (InputField input in inputs)
            SaveField(character, input != null ? input.transform : null, input != null ? input.text : "");
        foreach (TMP_InputField input in tmpInputs)
            SaveField(character, input != null ? input.transform : null, input != null ? input.text : "");
    }

    public static void Load(CharacterData character, IEnumerable<InputField> inputs,
        IEnumerable<TMP_InputField> tmpInputs)
    {
        if (character == null)
            return;

        foreach (InputField input in inputs)
        {
            if (input == null)
                continue;

            string key = GetKey(input.transform);
            if (string.IsNullOrEmpty(key))
                continue;

            if (character.HasSharedString(key))
                input.SetTextWithoutNotify(character.GetSharedString(key, ""));
            else
                character.SetSharedString(key, input.text);
        }

        foreach (TMP_InputField input in tmpInputs)
        {
            if (input == null)
                continue;

            string key = GetKey(input.transform);
            if (string.IsNullOrEmpty(key))
                continue;

            if (character.HasSharedString(key))
                input.SetTextWithoutNotify(character.GetSharedString(key, ""));
            else
                character.SetSharedString(key, input.text);
        }
    }

    public static void Clear(CharacterData character, IEnumerable<InputField> inputs,
        IEnumerable<TMP_InputField> tmpInputs)
    {
        if (character == null || !ContainsSharedField(inputs, tmpInputs))
            return;

        character.DeleteSharedString("SharedInput_magMod");
        character.DeleteSharedString("SharedInput_slogSpas");
    }

    public static string GetKey(Transform transform)
    {
        while (transform != null)
        {
            if (SceneObjectName.Matches(transform.name, "magMod"))
                return "SharedInput_magMod";
            if (SceneObjectName.Matches(transform.name, "slogSpas"))
                return "SharedInput_slogSpas";
            transform = transform.parent;
        }

        return "";
    }

    private static void SaveField(CharacterData character, Transform transform, string value)
    {
        string key = GetKey(transform);
        if (!string.IsNullOrEmpty(key))
            character.SetSharedString(key, value);
    }

    private static bool ContainsSharedField(IEnumerable<InputField> inputs,
        IEnumerable<TMP_InputField> tmpInputs)
    {
        foreach (InputField input in inputs)
            if (!string.IsNullOrEmpty(GetKey(input != null ? input.transform : null)))
                return true;

        foreach (TMP_InputField input in tmpInputs)
            if (!string.IsNullOrEmpty(GetKey(input != null ? input.transform : null)))
                return true;

        return false;
    }
}
