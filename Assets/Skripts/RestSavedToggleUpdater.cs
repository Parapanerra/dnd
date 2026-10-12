using System;
using System.Collections.Generic;

internal sealed class RestSavedToggleUpdater
{
    private const string StablePrefix = "StableField_v1_";
    private const string AliasPrefix = "Toggle_";
    private const string RolePrefix = "RestRole_";
    private readonly LegacyRestToggleIdMap aliases = new LegacyRestToggleIdMap();

    internal void Clear(CharacterSceneData data, string role, string panelPath,
        int minToggleNumber = int.MinValue, int maxToggleNumber = int.MaxValue)
    {
        if (data == null || data.intData == null)
            return;

        var stableIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (IntSaveEntry entry in GetLegacyEntries(data, panelPath, minToggleNumber, maxToggleNumber))
        {
            entry.value = 0;
            if (aliases.TryGetStableId(entry.key, out string id))
                stableIds.Add(id);
        }

        foreach (RoleToggle toggle in GetRoleToggles(data, role, minToggleNumber, maxToggleNumber))
            stableIds.Add(toggle.id);

        foreach (string id in stableIds)
            data.SetInt(StablePrefix + id, 0);
    }

    internal void ReduceExhaustionByOne(CharacterSceneData data, string panelPath, int lastToggleNumber)
    {
        if (data == null || data.intData == null)
            return;

        List<RoleToggle> mapped = GetRoleToggles(data, "Exhaustion", 0, lastToggleNumber);
        mapped.Sort((left, right) => left.number.CompareTo(right.number));
        if (mapped.Count > 0)
        {
            int checkedCount = 0;
            foreach (RoleToggle toggle in mapped)
                if (data.GetInt(StablePrefix + toggle.id) != 0)
                    checkedCount++;

            if (checkedCount == 0)
                return;

            RoleToggle selected = mapped[Math.Min(checkedCount - 1, mapped.Count - 1)];
            data.SetInt(StablePrefix + selected.id, 0);
            foreach (IntSaveEntry alias in GetLegacyEntries(data, panelPath, selected.number, selected.number))
                alias.value = 0;
            return;
        }

        List<IntSaveEntry> entries = GetLegacyEntries(data, panelPath, 0, lastToggleNumber);
        entries.Sort((left, right) => SceneObjectName.ToggleNumber(left.key).CompareTo(SceneObjectName.ToggleNumber(right.key)));
        int count = 0;
        foreach (IntSaveEntry entry in entries)
            if (entry.value != 0)
                count++;

        if (count == 0)
            return;

        IntSaveEntry last = entries[Math.Min(count - 1, entries.Count - 1)];
        last.value = 0;
        if (aliases.TryGetStableId(last.key, out string stableId))
            data.SetInt(StablePrefix + stableId, 0);
    }

    private List<IntSaveEntry> GetLegacyEntries(CharacterSceneData data, string panelPath, int min, int max)
    {
        var result = new List<IntSaveEntry>();
        if (string.IsNullOrEmpty(panelPath))
            return result;

        string prefix = AliasPrefix + panelPath + "/";
        foreach (IntSaveEntry entry in data.intData)
            if (entry != null && entry.key != null && entry.key.StartsWith(prefix, StringComparison.Ordinal))
            {
                int number = SceneObjectName.ToggleNumber(entry.key);
                if (number >= min && number <= max)
                    result.Add(entry);
            }
        return result;
    }

    private List<RoleToggle> GetRoleToggles(CharacterSceneData data, string role, int min, int max)
    {
        var result = new List<RoleToggle>();
        if (data.stringData == null || string.IsNullOrEmpty(role))
            return result;

        foreach (StringSaveEntry entry in data.stringData)
        {
            if (entry == null || entry.key == null || !entry.key.StartsWith(RolePrefix, StringComparison.Ordinal) || entry.value == null)
                continue;

            int separator = entry.value.LastIndexOf('|');
            if (separator < 0 || !string.Equals(entry.value.Substring(0, separator), role, StringComparison.Ordinal) ||
                !int.TryParse(entry.value.Substring(separator + 1), out int number) || number < min || number > max)
                continue;

            result.Add(new RoleToggle(entry.key.Substring(RolePrefix.Length), number));
        }
        return result;
    }

    private readonly struct RoleToggle
    {
        public readonly string id;
        public readonly int number;

        public RoleToggle(string id, int number)
        {
            this.id = id;
            this.number = number;
        }
    }
}
