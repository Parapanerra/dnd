using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

public class DeathSaveToggleSequence : MonoBehaviour
{
    [SerializeField] private Toggle[] orderedToggles = Array.Empty<Toggle>();
    [SerializeField] private bool clampToActiveToggles;

    private readonly List<UnityAction<bool>> listeners = new List<UnityAction<bool>>();
    private int currentCount;
    private bool isApplying;

    public static void ConfigureScene()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform transform in transforms)
        {
            if (IsDeathCheckContainer(transform.name))
            {
                ConfigureGroup(transform, "uspih", false);
                ConfigureGroup(transform, "proval", false);
                continue;
            }

            if (NameMatches(transform.name, "vtoma"))
                ConfigureGroup(transform, true, false);

            if (NameMatches(transform.name, "resursClas"))
            {
                ConfigureExactOrBaseChildGroup(transform, "Panel", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (1)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (2)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (3)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (4)", true, false);
                ConfigureExactOrBaseChildGroup(transform, "Panel (5)", true, false, 7);
                ConfigureExactOrBaseChildGroup(transform, "Panel (8)", true, false);
            }
        }
    }

    private static void ConfigureGroup(Transform container, string groupName, bool ascending)
    {
        foreach (Transform child in container)
        {
            if (!NameMatches(child.name, groupName))
                continue;

            ConfigureGroup(child, ascending, false);
        }
    }

    private static void ConfigureExactOrBaseChildGroup(Transform container, string exactName, bool ascending, bool clampToActive, int maxToggleNumber = int.MaxValue)
    {
        foreach (Transform child in container)
        {
            if (!child.name.Equals(exactName, StringComparison.OrdinalIgnoreCase))
                continue;

            ConfigureGroup(child, ascending, clampToActive, maxToggleNumber);
            return;
        }
    }

    private static void ConfigureGroup(Transform groupRoot, bool ascending, bool clampToActive, int maxToggleNumber = int.MaxValue)
    {
        List<Toggle> groupToggles = new List<Toggle>();
        foreach (Toggle toggle in groupRoot.GetComponentsInChildren<Toggle>(true))
            if (IsSequenceToggle(toggle, groupRoot, maxToggleNumber))
                groupToggles.Add(toggle);

        if (groupToggles.Count == 0)
            return;

        groupToggles.Sort((left, right) => CompareTogglesBySequenceOrder(left, right, ascending));

        DeathSaveToggleSequence sequence = groupRoot.GetComponent<DeathSaveToggleSequence>();
        if (sequence == null)
            sequence = groupRoot.gameObject.AddComponent<DeathSaveToggleSequence>();

        sequence.Configure(groupToggles.ToArray(), clampToActive);
    }

    private void Configure(Toggle[] toggles, bool clampToActive)
    {
        if (orderedToggles != null)
            for (int i = 0; i < orderedToggles.Length && i < listeners.Count; i++)
                if (orderedToggles[i] != null && listeners[i] != null)
                    orderedToggles[i].onValueChanged.RemoveListener(listeners[i]);

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

    private static int CompareTogglesBySequenceOrder(Toggle left, Toggle right, bool ascending)
    {
        int leftIndex = GetToggleNumber(left != null ? left.name : "");
        int rightIndex = GetToggleNumber(right != null ? right.name : "");
        return ascending ? leftIndex.CompareTo(rightIndex) : rightIndex.CompareTo(leftIndex);
    }

    private static int GetToggleNumber(string name)
    {
        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');
        if (open >= 0 && close > open && int.TryParse(name.Substring(open + 1, close - open - 1), out int number))
            return number;

        return 0;
    }

    private static bool IsSequenceToggle(Toggle toggle, Transform groupRoot, int maxToggleNumber)
    {
        if (toggle == null || !NameMatches(toggle.name, "Toggle"))
            return false;

        if (GetToggleNumber(toggle.name) > maxToggleNumber)
            return false;

        Transform current = toggle.transform;
        while (current != null && current != groupRoot)
        {
            if (current.GetComponent<Dropdown>() != null || current.GetComponent<TMP_Dropdown>() != null)
                return false;

            current = current.parent;
        }

        return true;
    }

    private static bool IsDeathCheckContainer(string name)
    {
        return NameMatches(name, "deadChekBox") || NameMatches(name, "deadCheckBox");
    }

    private static bool NameMatches(string actualName, string expectedName)
    {
        return GetBaseName(actualName).Equals(expectedName, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseName(string name)
    {
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
        return suffixStart >= 0 ? name.Substring(0, suffixStart) : name;
    }
}
