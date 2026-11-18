using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    // Only generated usage counters are registered; spell preparation is never reset.
    internal static class CreationResourceRecovery
    {
        private const string Key = "CreationResourceRecovery_v1";
        [Serializable] private sealed class Group
        {
            public string key;
            public bool shortRest;
            public bool oneOnShortRest;
            public int amountOnShortRest;
            public bool oncePerLongRest;
            public int pointsMaximum;
            public CreationSheetBindings.Entry[] fields;
        }
        [Serializable] private sealed class Groups { public Group[] groups = Array.Empty<Group>(); }

        private static Group[] Read(string json)
        {
            if (string.IsNullOrEmpty(json)) return Array.Empty<Group>();
            if (json.Contains("\"groups\"")) return JsonUtility.FromJson<Groups>(json)?.groups ?? Array.Empty<Group>();
            return new[] { JsonUtility.FromJson<Group>(json) };
        }

        internal static void Register(CharacterData character, CreationSheetBindings bindings, string key, bool shortRest, bool oneOnShortRest = false)
        {
            var groups = Read(character.GetSharedString(Key, "")).Where(group => group != null && group.key != key).ToList();
            groups.Add(new Group { key = key, shortRest = shortRest, oneOnShortRest = oneOnShortRest,
                fields = bindings.Entries.Where(entry => entry.key == key && entry.collection == "toggleData").ToArray() });
            character.SetSharedString(Key, JsonUtility.ToJson(new Groups { groups = groups.ToArray() }));
        }

        internal static void RegisterPool(CharacterData character, CreationSheetBindings bindings, string key, string[] sourceKeys, bool shortRest, bool oneOnShortRest)
        {
            var groups = Read(character.GetSharedString(Key, "")).Where(group => group != null && group.key != key).ToList();
            groups.Add(new Group { key = key, shortRest = shortRest, oneOnShortRest = oneOnShortRest,
                fields = bindings.Entries.Where(entry => sourceKeys.Contains(entry.key) && entry.collection == "toggleData").ToArray() });
            character.SetSharedString(Key, JsonUtility.ToJson(new Groups { groups = groups.ToArray() }));
        }

        internal static void RegisterPartial(CharacterData character, CreationSheetBindings bindings, string key, int amount, bool oncePerLongRest)
        {
            Register(character, bindings, key, amount > 0);
            var groups = Read(character.GetSharedString(Key, ""));
            var group = groups.Single(item => item.key == key);
            group.amountOnShortRest = Math.Max(0, amount); group.oncePerLongRest = oncePerLongRest;
            character.SetSharedString(Key, JsonUtility.ToJson(new Groups { groups = groups }));
        }

        internal static bool HasGroup(CharacterData character, string key) => character != null &&
            Read(character.GetSharedString(Key, "")).Any(group => group != null && group.key == key);

        internal static void Unregister(CharacterData character, string key)
        {
            var groups = Read(character.GetSharedString(Key, "")).Where(group => group != null && group.key != key).ToArray();
            character.SetSharedString(Key, JsonUtility.ToJson(new Groups { groups = groups }));
        }

        internal static void RegisterPoints(CharacterData character, CreationSheetBindings bindings, string key, int maximum)
        {
            var groups = Read(character.GetSharedString(Key, "")).Where(group => group != null && group.key != key).ToList();
            groups.Add(new Group { key = key, pointsMaximum = maximum,
                fields = bindings.Entries.Where(entry => entry.key == key && entry.collection == "inputData").ToArray() });
            character.SetSharedString(Key, JsonUtility.ToJson(new Groups { groups = groups.ToArray() }));
        }

        internal static void Apply(CharacterData character, bool longRest,
            Func<CreationSheetBindings.Entry, bool> includeField = null,
            Func<CreationSheetBindings.Entry, bool> readLiveUsage = null,
            Action<CreationSheetBindings.Entry, int> updateLiveValue = null)
        {
            if (character == null) return;
            string json = character.GetSharedString(Key, "");
            if (json.Length == 0) return;
            foreach (var group in Read(json))
            {
                if (group == null || group.fields == null || !longRest && !group.shortRest) continue;
                string usageKey = "CreationShortRecoveryUsed_" + group.key;
                if (longRest && group.oncePerLongRest) character.SetSharedString(usageKey, "0");
                if (!longRest && group.oncePerLongRest && character.GetSharedString(usageKey, "0") == "1") continue;
                int recovered = 0;
                if (group.pointsMaximum > 0)
                {
                    foreach (var field in group.fields)
                    {
                        if (includeField != null && !includeField(field)) continue;
                        var data = character.GetSceneData(field.scene);
                        string value = group.pointsMaximum.ToString();
                        data.SetString("StableField_v1_" + field.stableId, value);
                        if (field.index >= 0 && field.index < data.inputData.Count) data.inputData[field.index] = value;
                        updateLiveValue?.Invoke(field, group.pointsMaximum);
                    }
                    continue;
                }
                foreach (var field in group.fields)
                {
                    if (includeField != null && !includeField(field)) continue;
                    var data = character.GetSceneData(field.scene);
                    bool consumed = readLiveUsage != null ? readLiveUsage(field) :
                        data.HasInt("StableField_v1_" + field.stableId) ? data.GetInt("StableField_v1_" + field.stableId) != 0 :
                        field.index >= 0 && field.index < data.toggleData.Count && data.toggleData[field.index];
                    if (!longRest && (group.oneOnShortRest || group.amountOnShortRest > 0) && !consumed) continue;
                    data.SetInt("StableField_v1_" + field.stableId, 0);
                    if (field.index >= 0 && field.index < data.toggleData.Count) data.toggleData[field.index] = false;
                    updateLiveValue?.Invoke(field, 0);
                    if (!longRest && consumed) recovered++;
                    if (!longRest && (group.oneOnShortRest || group.amountOnShortRest > 0 && recovered >= group.amountOnShortRest)) break;
                }
                if (!longRest && group.oncePerLongRest && recovered > 0) character.SetSharedString(usageKey, "1");
            }
        }
    }
}
