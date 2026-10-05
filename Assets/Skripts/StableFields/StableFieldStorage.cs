using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class StableFieldStorage
{
    private const string Prefix = "StableField_v1_";
    private static string Key(PersistentFieldId field) => Prefix + field.Id;

    public static string ReadText(CharacterSceneData data, Component control, string legacyDefault)
    {
        var field = PersistentFieldId.For(control);
        if (field == null) return legacyDefault;
        string key = Key(field);
        if (data.HasString(key)) return data.GetString(key);
        int index = field.LegacyIndex;
        if (index < 0 || index >= data.inputData.Count) return "";
        string value = data.inputData[index];
        data.SetString(key, value);
        data.stableFieldVersion = 1;
        return value;
    }

    public static float ReadFloat(CharacterSceneData data, Component control, float legacyDefault)
    {
        var field = PersistentFieldId.For(control);
        if (field == null) return legacyDefault;
        if (data.floatData == null) data.floatData = new List<FloatSaveEntry>();
        var entry = data.floatData.Find(item => item.key == Key(field));
        if (entry != null) return entry.value;
        int index = field.LegacyIndex;
        if (index < 0 || index >= data.sliderData.Count) return 0;
        float value = data.sliderData[index];
        SetFloat(data, field, value);
        data.stableFieldVersion = 1;
        return value;
    }

    public static int ReadInt(CharacterSceneData data, Component control, int legacyDefault)
    {
        var field = PersistentFieldId.For(control);
        if (field == null) return legacyDefault;
        string key = Key(field);
        if (data.HasInt(key)) return data.GetInt(key);
        int index = field.LegacyIndex;
        int value;
        // A0 verified positional values even where hierarchy-derived aliases conflict.
        if (field.LegacyCollection == "toggleData" && index >= 0 && index < data.toggleData.Count)
            value = data.toggleData[index] ? 1 : 0;
        else if (field.LegacyCollection == "dropdownData" && index >= 0 && index < data.dropdownData.Count)
            value = data.dropdownData[index];
        else if (!string.IsNullOrEmpty(field.LegacyKey) && data.HasInt(field.LegacyKey))
            value = data.GetInt(field.LegacyKey);
        else
            return 0;
        data.SetInt(key, value);
        data.stableFieldVersion = 1;
        return value;
    }

    public static void Save(CharacterSceneData data, IEnumerable<Component> controls)
    {
        foreach (var control in controls)
        {
            var field = PersistentFieldId.For(control);
            if (field == null) continue;
            data.stableFieldVersion = 1;
            string key = Key(field);
            int index = field.LegacyIndex;
            if (control is InputField input)
            {
                data.SetString(key, input.text);
                WriteLegacy(data.inputData, index, input.text);
            }
            else if (control is TMP_InputField tmpInput)
            {
                data.SetString(key, tmpInput.text);
                WriteLegacy(data.inputData, index, tmpInput.text);
            }
            else if (control is Slider slider)
            {
                SetFloat(data, field, slider.value);
                WriteLegacy(data.sliderData, index, slider.value);
            }
            else if (control is Toggle toggle)
            {
                data.SetInt(key, toggle.isOn ? 1 : 0);
                WriteLegacy(data.toggleData, index, toggle.isOn);
            }
            else if (control is Dropdown dropdown)
            {
                data.SetInt(key, dropdown.value);
                WriteLegacy(data.dropdownData, index, dropdown.value);
            }
            else if (control is TMP_Dropdown tmpDropdown)
            {
                data.SetInt(key, tmpDropdown.value);
                WriteLegacy(data.dropdownData, index, tmpDropdown.value);
            }
        }
    }

    private static void SetFloat(CharacterSceneData data, PersistentFieldId field, float value)
    {
        if (data.floatData == null) data.floatData = new List<FloatSaveEntry>();
        var entry = data.floatData.Find(item => item.key == Key(field));
        if (entry == null)
        {
            entry = new FloatSaveEntry { key = Key(field) };
            data.floatData.Add(entry);
        }
        entry.value = value;
    }

    private static void WriteLegacy<T>(List<T> values, int index, T value)
    {
        if (index < 0) return;
        while (values.Count <= index) values.Add(default(T));
        values[index] = value;
    }
}
