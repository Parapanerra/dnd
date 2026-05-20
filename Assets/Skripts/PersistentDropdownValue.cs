using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PersistentDropdownValue : MonoBehaviour
{
    private const string KeyPrefix = "PersistentDropdown_";

    private Dropdown dropdown;
    private TMP_Dropdown tmpDropdown;
    private string controlKey;
    private bool isLoading;

    private void Awake()
    {
        dropdown = GetComponent<Dropdown>();
        tmpDropdown = GetComponent<TMP_Dropdown>();
        controlKey = KeyPrefix + GetControlPath(transform);
    }

    private void OnEnable()
    {
        LoadValue();

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        if (tmpDropdown != null)
        {
            tmpDropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            tmpDropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    private void OnDisable()
    {
        SaveValue();
    }

    public void Reload()
    {
        LoadValue();
    }

    private void OnDropdownChanged(int value)
    {
        if (!isLoading)
            SaveValue();
    }

    private void SaveValue()
    {
        if (DndSaveManager.Instance == null)
            return;

        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData();
        if (sceneData == null)
            return;

        if (dropdown != null)
            sceneData.SetInt(controlKey, dropdown.value);
        else if (tmpDropdown != null)
            sceneData.SetInt(controlKey, tmpDropdown.value);

        DndSaveManager.Instance.SaveData();
    }

    private void LoadValue()
    {
        if (DndSaveManager.Instance == null)
            return;

        CharacterSceneData sceneData = DndSaveManager.Instance.GetActiveSceneData(false);
        if (sceneData == null || !sceneData.HasInt(controlKey))
            return;

        int value = sceneData.GetInt(controlKey);
        isLoading = true;
        try
        {
            if (dropdown != null)
            {
                dropdown.SetValueWithoutNotify(value);
                dropdown.RefreshShownValue();
            }
            else if (tmpDropdown != null)
            {
                tmpDropdown.SetValueWithoutNotify(value);
                tmpDropdown.RefreshShownValue();
            }
        }
        finally
        {
            isLoading = false;
        }
    }

    private string GetControlPath(Transform current)
    {
        string path = current.GetSiblingIndex().ToString("D4") + "_" + current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = current.GetSiblingIndex().ToString("D4") + "_" + current.name + "/" + path;
        }

        return path;
    }
}
