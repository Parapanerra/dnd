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
    private List<InputField> inputFields = new List<InputField>();
    private List<TMP_InputField> tmpInputFields = new List<TMP_InputField>();
    private List<Toggle> toggles = new List<Toggle>();
    private List<Slider> sliders = new List<Slider>();
    private List<Dropdown> dropdowns = new List<Dropdown>();
    private List<TMP_Dropdown> tmpDropdowns = new List<TMP_Dropdown>();
    private List<Button> resetButtons = new List<Button>();

    public IReadOnlyList<InputField> InputFields => inputFields.AsReadOnly();
    public IReadOnlyList<TMP_InputField> TmpInputFields => tmpInputFields.AsReadOnly();
    public IReadOnlyList<Toggle> Toggles => toggles.AsReadOnly();
    public IReadOnlyList<Slider> Sliders => sliders.AsReadOnly();
    public IReadOnlyList<Dropdown> Dropdowns => dropdowns.AsReadOnly();
    public IReadOnlyList<TMP_Dropdown> TmpDropdowns => tmpDropdowns.AsReadOnly();
    public IReadOnlyList<Button> ResetButtons => resetButtons.AsReadOnly();
    public void Collect(Func<Transform, bool> isManagedByHealthBar, bool excludeDropdownTemplates)
    {
        inputFields = new List<InputField>(UnityEngine.Object.FindObjectsByType<InputField>(FindObjectsInactive.Include));
        tmpInputFields = new List<TMP_InputField>(UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include));
        toggles = new List<Toggle>(UnityEngine.Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include));
        sliders = new List<Slider>(UnityEngine.Object.FindObjectsByType<Slider>(FindObjectsInactive.Include));
        dropdowns = new List<Dropdown>(UnityEngine.Object.FindObjectsByType<Dropdown>(FindObjectsInactive.Include));
        tmpDropdowns = new List<TMP_Dropdown>(UnityEngine.Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include));
        resetButtons = new List<Button>(UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include));

        inputFields.RemoveAll(inputField => isManagedByHealthBar(inputField != null ? inputField.transform : null));
        tmpInputFields.RemoveAll(inputField => isManagedByHealthBar(inputField != null ? inputField.transform : null));
        if (excludeDropdownTemplates)
            toggles.RemoveAll(toggle => IsDropdownTemplatePart(toggle != null ? toggle.transform : null));
        sliders.RemoveAll(slider => isManagedByHealthBar(slider != null ? slider.transform : null));

        inputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpInputFields.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        toggles.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        sliders.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        dropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
        tmpDropdowns.Sort((a, b) => string.Compare(GetControlPath(a.transform), GetControlPath(b.transform), StringComparison.Ordinal));
    }

    public void Save(CharacterSceneData sceneData, Action afterTextFields)
    {
        sceneData.inputData.Clear();
        foreach (InputField inputField in inputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        foreach (TMP_InputField inputField in tmpInputFields)
            sceneData.inputData.Add(inputField != null ? inputField.text : "");

        afterTextFields();
        sceneData.toggleData.Clear();
        foreach (Toggle toggle in toggles)
        {
            bool isOn = toggle != null && toggle.isOn;
            sceneData.toggleData.Add(isOn);
            if (toggle != null)
                sceneData.SetInt(ToggleKeyPrefix + GetControlPath(toggle.transform), isOn ? 1 : 0);
        }

        sceneData.sliderData.Clear();
        foreach (Slider slider in sliders)
            sceneData.sliderData.Add(slider != null ? slider.value : 0f);

        sceneData.dropdownData.Clear();
        foreach (Dropdown dropdown in dropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(DropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        foreach (TMP_Dropdown dropdown in tmpDropdowns)
        {
            sceneData.dropdownData.Add(dropdown != null ? dropdown.value : 0);
            if (dropdown != null)
                sceneData.SetInt(TmpDropdownKeyPrefix + GetControlPath(dropdown.transform), dropdown.value);
        }

        StableFieldStorage.Save(sceneData, inputFields);
        StableFieldStorage.Save(sceneData, tmpInputFields);
        StableFieldStorage.Save(sceneData, toggles);
        StableFieldStorage.Save(sceneData, sliders);
        StableFieldStorage.Save(sceneData, dropdowns);
        StableFieldStorage.Save(sceneData, tmpDropdowns);
    }

    public void Load(CharacterSceneData sceneData)
    {
        bool previous = isApplying;
        isApplying = true;
        try
        {
            int dataIndex = 0;

            for (int i = 0; i < inputFields.Count; i++, dataIndex++)
                if (inputFields[i] != null)
                    inputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, inputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < tmpInputFields.Count; i++, dataIndex++)
                if (tmpInputFields[i] != null)
                    tmpInputFields[i].SetTextWithoutNotify(StableFieldStorage.ReadText(sceneData, tmpInputFields[i], dataIndex < sceneData.inputData.Count ? sceneData.inputData[dataIndex] : ""));

            for (int i = 0; i < toggles.Count; i++)
                if (toggles[i] != null)
                {
                    string key = ToggleKeyPrefix + GetControlPath(toggles[i].transform);
                    bool value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key) != 0
                        : i < sceneData.toggleData.Count && sceneData.toggleData[i];
                    toggles[i].SetIsOnWithoutNotify(StableFieldStorage.ReadInt(sceneData, toggles[i], value ? 1 : 0) != 0);
                }

            for (int i = 0; i < sliders.Count; i++)
                if (sliders[i] != null)
                    sliders[i].SetValueWithoutNotify(StableFieldStorage.ReadFloat(sceneData, sliders[i], i < sceneData.sliderData.Count ? sceneData.sliderData[i] : 0f));

            for (int i = 0; i < dropdowns.Count; i++)
                if (dropdowns[i] != null)
                {
                    string key = DropdownKeyPrefix + GetControlPath(dropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i < sceneData.dropdownData.Count ? sceneData.dropdownData[i] : 0;
                    dropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, dropdowns[i], value));
                    dropdowns[i].RefreshShownValue();
                }

            int tmpDropdownOffset = dropdowns.Count;
            for (int i = 0; i < tmpDropdowns.Count; i++)
                if (tmpDropdowns[i] != null)
                {
                    string key = TmpDropdownKeyPrefix + GetControlPath(tmpDropdowns[i].transform);
                    int value = sceneData.HasInt(key)
                        ? sceneData.GetInt(key)
                        : i + tmpDropdownOffset < sceneData.dropdownData.Count ? sceneData.dropdownData[i + tmpDropdownOffset] : 0;
                    tmpDropdowns[i].SetValueWithoutNotify(StableFieldStorage.ReadInt(sceneData, tmpDropdowns[i], value));
                    tmpDropdowns[i].RefreshShownValue();
                }
        }
        finally
        {
            isApplying = previous;
        }
    }

    public void Subscribe(Action save, Action flush = null)
    {
        foreach (InputField inputField in inputFields)
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
                inputField.onEndEdit.AddListener(delegate { if (!isApplying) flush?.Invoke(); });
            }

        foreach (TMP_InputField inputField in tmpInputFields)
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
                inputField.onEndEdit.AddListener(delegate { if (!isApplying) flush?.Invoke(); });
            }

        foreach (Toggle toggle in toggles)
            if (toggle != null)
                toggle.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (Slider slider in sliders)
            if (slider != null)
                slider.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (Dropdown dropdown in dropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { if (!isApplying) save(); });

        foreach (TMP_Dropdown dropdown in tmpDropdowns)
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(delegate { if (!isApplying) save(); });
    }

    public void Reset()
    {
        bool previous = isApplying;
        isApplying = true;
        try
        {
            foreach (InputField inputField in inputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

            foreach (TMP_InputField inputField in tmpInputFields)
                if (inputField != null)
                    inputField.SetTextWithoutNotify("");

            foreach (Toggle toggle in toggles)
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(false);

            foreach (Slider slider in sliders)
                if (slider != null)
                    slider.SetValueWithoutNotify(0f);

            foreach (Dropdown dropdown in dropdowns)
                if (dropdown != null)
                {
                    dropdown.SetValueWithoutNotify(0);
                    dropdown.RefreshShownValue();
                }

            foreach (TMP_Dropdown dropdown in tmpDropdowns)
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
        return StableFieldPath.For(transform);
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
