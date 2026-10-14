using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Selects resource toggles; dropdown templates are UI controls, not rest resources.
internal sealed class RestSceneToggleFinder
{
    internal List<Toggle> Find(Transform panel, int minNumber = int.MinValue, int maxNumber = int.MaxValue)
    {
        var result = new List<Toggle>();
        if (panel == null)
            return result;

        foreach (Toggle toggle in panel.GetComponentsInChildren<Toggle>(true))
        {
            if (toggle == null || (toggle.GetComponent<ResourceToggleMarker>() == null &&
                !SceneObjectName.Matches(toggle.name, "Toggle")) || IsInsideDropdown(toggle.transform, panel))
                continue;

            int number = ResourceToggleMarker.NumberOf(toggle);
            if (number >= minNumber && number <= maxNumber)
                result.Add(toggle);
        }

        return result;
    }

    private bool IsInsideDropdown(Transform transform, Transform panel)
    {
        for (Transform current = transform; current != null && current != panel; current = current.parent)
            if (current.GetComponent<Dropdown>() != null || current.GetComponent<TMP_Dropdown>() != null)
                return true;

        return false;
    }
}
