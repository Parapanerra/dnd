using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DropdownManager : MonoBehaviour
{
    [Serializable]
    public class DropdownConfig
    {
        [SerializeField] private Dropdown dropdown;
        [SerializeField] private List<GameObject> tangles;

        public void Bind()
        {
            if (dropdown == null)
                return;

            dropdown.onValueChanged.RemoveListener(OnValueChanged);
            dropdown.onValueChanged.AddListener(OnValueChanged);
            Apply();
        }

        public void Unbind()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnValueChanged);
        }

        public void Apply()
        {
            if (dropdown == null || tangles == null)
                return;

            int selectedNumber = dropdown.value;
            for (int i = 0; i < tangles.Count; i++)
            {
                GameObject group = tangles[i];
                if (group == null)
                    continue;

                if (i < selectedNumber)
                    group.SetActive(true);
                else
                {
                    ClearToggles(group);
                    group.SetActive(false);
                }
            }
        }

        private void OnValueChanged(int value)
        {
            Apply();
        }

        private static void ClearToggles(GameObject root)
        {
            foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
                if (toggle != null && toggle.isOn)
                    toggle.isOn = false;
        }
    }

    [SerializeField] private List<DropdownConfig> dropdownConfigs;

    private void Start()
    {
        if (dropdownConfigs == null)
            return;

        foreach (DropdownConfig config in dropdownConfigs)
            config?.Bind();
    }

    private void OnDestroy()
    {
        if (dropdownConfigs == null)
            return;

        foreach (DropdownConfig config in dropdownConfigs)
            config?.Unbind();
    }

    public void RefreshAll()
    {
        if (dropdownConfigs == null)
            return;

        foreach (DropdownConfig config in dropdownConfigs)
            config?.Apply();
    }
}