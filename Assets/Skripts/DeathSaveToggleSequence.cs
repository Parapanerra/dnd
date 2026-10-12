using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DeathSaveToggleSequence : MonoBehaviour
{
    [SerializeField] private Toggle[] orderedToggles = Array.Empty<Toggle>();
    [SerializeField] private bool clampToActiveToggles;

    private readonly List<UnityAction<bool>> listeners = new List<UnityAction<bool>>();
    private int currentCount;
    private bool isApplying;
    private ExhaustionEffects exhaustionEffects;

    internal void Configure(Toggle[] toggles, bool clampToActive, ExhaustionEffects exhaustion)
    {
        if (orderedToggles != null)
            for (int i = 0; i < orderedToggles.Length && i < listeners.Count; i++)
                if (orderedToggles[i] != null && listeners[i] != null)
                    orderedToggles[i].onValueChanged.RemoveListener(listeners[i]);

        if (IsExhaustionGroup() && exhaustionEffects == null)
            exhaustionEffects = exhaustion;

        orderedToggles = toggles ?? Array.Empty<Toggle>();
        clampToActiveToggles = clampToActive;
        listeners.Clear();

        for (int i = 0; i < orderedToggles.Length; i++)
        {
            int index = i;
            Toggle toggle = orderedToggles[i];
            UnityAction<bool> listener = value => OnToggleChanged(index, value);
            listeners.Add(listener);
            if (toggle != null)
                toggle.onValueChanged.AddListener(listener);
        }

        ApplyCount(GetCheckedCount());
    }

    private void OnToggleChanged(int clickedIndex, bool isOn)
    {
        if (isApplying)
            return;

        int activeCount = GetAllowedSequenceCount();
        int newCount = isOn
            ? Mathf.Min(currentCount + 1, activeCount)
            : Mathf.Clamp(clickedIndex, 0, activeCount);

        ApplyCount(newCount);
    }

    private int GetCheckedCount()
    {
        int count = 0;
        foreach (Toggle toggle in orderedToggles)
            if (toggle != null && toggle.isOn)
                count++;

        return count;
    }

    private void ApplyCount(int count)
    {
        isApplying = true;
        try
        {
            int activeCount = GetAllowedSequenceCount();
            count = Mathf.Clamp(count, 0, activeCount);
            currentCount = count;
            for (int i = 0; i < orderedToggles.Length; i++)
            {
                Toggle toggle = orderedToggles[i];
                if (toggle != null)
                    toggle.SetIsOnWithoutNotify(i < count && i < activeCount);
            }
        }
        finally
        {
            isApplying = false;
        }

        if (IsExhaustionGroup() && exhaustionEffects != null)
            exhaustionEffects.Apply(count);
    }

    private bool IsExhaustionGroup()
    {
        SceneRoleMarker marker = GetComponent<SceneRoleMarker>();
        return marker != null ? marker.Role == SceneRole.Exhaustion : SceneObjectName.Matches(name, "vtoma");
    }

    private int GetAllowedSequenceCount()
    {
        if (!clampToActiveToggles)
            return orderedToggles.Length;

        int count = 0;
        foreach (Toggle toggle in orderedToggles)
        {
            if (toggle != null && toggle.gameObject.activeInHierarchy)
                count++;
        }

        return count;
    }


}
