using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shared standard-field behavior. Scene-specific identity, health, inventory and disk I/O
// remain in the existing MonoBehaviour adapters.
public sealed class SceneSaveController
{
    private bool isApplying;
    private const string DropdownKeyPrefix = "Dropdown_";
    private const string TmpDropdownKeyPrefix = "TMPDropdown_";
    private const string ToggleKeyPrefix = "Toggle_";
    public List<InputField> InputFields { get; private set; } = new List<InputField>();
    public List<TMP_InputField> TmpInputFields { get; private set; } = new List<TMP_InputField>();
    public List<Toggle> Toggles { get; private set; } = new List<Toggle>();
    public List<Slider> Sliders { get; private set; } = new List<Slider>();
    public List<Dropdown> Dropdowns { get; private set; } = new List<Dropdown>();
    public List<TMP_Dropdown> TmpDropdowns { get; private set; } = new List<TMP_Dropdown>();
    public List<Button> ResetButtons { get; private set; } = new List<Button>();

    public void Collect(Func<Transform, bool> isManagedByHealthBar, bool excludeDropdownTemplates)
    {
        InputFields = new List<InputField>(UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include));
        TmpInputFields = new List<TMP_InputField>(UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include));
        Toggles = new List<Toggle>(UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include));
        Sliders = new List<Slider>(UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include));
        Dropdowns = new List<Dropdown>(UnityEngine.Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include));
        TmpDropdowns = new List<TMP_Dropdown>(UnityEngine.Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include));
        ResetButtons = new List<Button>(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include));

        InputFields.RemoveAll(inputField => isManagedByHealthBar(inputField != null ? inputField.transform : null));
        TmpInputFields.RemoveAll(inputField => isManagedByHealthBar(inputField != null ? inputField.transform : null));
        if (excludeDropdownTemplates)
            Toggles.RemoveAll(toggle => IsDropdownTemplatePart(toggle != null ? toggle.transform : null));
        Sliders.RemoveAll(slider => isManagedByHealthBar(slider != null ? slider.transform : null));

        InputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        TmpInputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        Toggles.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        Sliders.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        Dropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        TmpDropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
    }

    public void Save(CharacterSceneData sceneData, Action afterTextFields)
    {
        sceneData.inputData.Clear();
        foreach (InputField inputField in InputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        foreach (TMP_InputField inputField in TmpInputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        afterTextFields();
        sceneData.toggleData.Clear();
        foreach (Toggle toggle in Toggles)
        {
            bool isOn = toggle != null && toggle.isOn;
            sceneData.toggleData.Add(isOn);
            if (toggle != null)
                sceneData.SetInt(ToggleKeyPrefix + GetControlPath(toggle.transform), isOn ? 1 : 0);
        }

        sceneData.sliderData.Clear();
        foreach (Slider slider in Sliders)
            sceneData.sliderData.Add(slider != null ? slider.value : 0f);

        sceneData.dropdownData.Clear();
        foreach (Dropdown dropdown in Dropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(DropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        foreach (TMP_Dropdown dropdown in TmpDropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(TmpDropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        StableFieldStorage.Save(sceneData, InputFields);
        StableFieldStorage.Save(sceneData, TmpInputFields);
        StableFieldStorage.Save(sceneData, Toggles);
        StableFieldStorage.Save(sceneData, Sliders);
        StableFieldStorage.Save(sceneData, Dropdowns);
        StableFieldStorage.Save(sceneData, TmpDropdowns);
    }

    public void Load(CharacterSceneData sceneData)
    {
        bool previous = isApplying;
        isApplying = true;
        try
        {
            int dataIndex = 0;

            for (int i = 0; i < InputFields.Count; i++, dataIndex++)
                if (InputFields[i] != null)
                    InputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, InputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < TmpInputFields.Count; i++, dataIndex++)
                if (TmpInputFields[i] != null)
                    TmpInputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, TmpInputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < Toggles.Count; i++)
                if (Toggles[i] != null)
                {
                    string key = ToggleKeyPrefix + GetControlPath(Toggles[i].transform);
                    bool value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key) != 0
                        : i < sceneData.toggleData.Count && sceneData.toggleData[i];
                    Toggles[i].SetIsOnWithoutNotify(StableFieldStorage.ReadInt(sceneData, Toggles[i], value ? 1 : 0) != 0);
                }

            for (int i = 0; i < Sliders.Count; i++)
                if (Sliders[i] != null)
                    Sliders[i].SetValueWithoutNotify(StableFieldStorage.ReadFloat(sceneData, Sliders[i], i < sceneData.sliderData.Count ? sceneData.sliderData[i] : 0f));

            for (int i = 0; i < Dropdowns.Count; i++)
                if (Dropdowns[i] != null)
                {
                    string key = DropdownKeyPrefix + GetControlPath(Dropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i < sceneData.dropdownData.Count ? sceneData.dropdownData[i] : 0;
                    Dropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, Dropdowns[i], value));
                    Dropdowns[i].RefreshShownValue();
                }

            int tmpDropdownOffset = Dropdowns.Count;
            for (int i = 0; i < TmpDropdowns.Count; i++)
                if (TmpDropdowns[i] != null)
                {
                    string key = TmpDropdownKeyPrefix + GetControlPath(TmpDropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i + tmpDropdownOffset < sceneData.dropdownData.Count ? sceneData.dropdownData[i + tmpDropdownOffset] : 0;
                    TmpDropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, TmpDropdowns[i], value));
                    TmpDropdowns[i].RefreshShownValue();
                }
        }
        finally
        {
            isApplying = previous;
        }
    }

    public void Subscribe(Action save, Action flush = null)
    {
        foreach (InputField inputField in InputFields)
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
                inputField.onEndEdit.AddListener(delegate { if (!isApplying) flush?.Invoke(); });
            }

        foreach (TMP_InputField inputField in TmpInputFields)
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
                inputField.onEndEdit.AddListener(delegate { if (!isApplying) flush?.Invoke(); });
            }

        foreach (Toggle toggle in Toggles)
            if (toggle != null)
                toggle.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (Slider slider in Sliders)
            if (slider != null)
                slider.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (Dropdown dropdown in Dropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (TMP_Dropdown dropdown in TmpDropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
    }

    public void Reset()
    {
        bool previous = isApplying;
        isApplying = true;
        try
        {
            foreach (InputField inputField in InputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

            foreach (TMP_InputField inputField in TmpInputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

            foreach (Toggle toggle in Toggles)
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(false);

            foreach (Slider slider in Sliders)
                if (slider != null)
                    slider.SetValueWithoutNotify(0f);

            foreach (Dropdown dropdown in Dropdowns)
                if (dropdown != null)
                {
                    dropdown.SetValueWithoutNotify(0);
                    dropdown.RefreshShownValue();
                }

            foreach (TMP_Dropdown dropdown in TmpDropdowns)
                if (dropdown != null)
                {
                    dropdown.SetValueWithoutNotify(0);
                    dropdown.RefreshShownValue();
                }
        }
        finally
        {
            isApplying = previous;
        }
    }

    private static string GetControlPath(Transform transform)
    {
        string path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.GetSiblingIndex().ToString("D4") + "_" + transform.name + "/" + path;
        }

        return path;
    }

    private static bool IsDropdownTemplatePart(Transform transform)
    {
        while (transform != null)
        {
            if (transform.GetComponent<Dropdown>() != null || transform.GetComponent<TMP_Dropdown>() != null)
                return true;

            transform = transform.parent;
        }

        return false;
    }
}
