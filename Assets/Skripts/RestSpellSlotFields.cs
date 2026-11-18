using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

internal static class RestSpellSlotFields
{
    [Serializable]
    internal sealed class Field
    {
        [SerializeField] private string scene;
        [SerializeField] private string stableId;
        [SerializeField] private string legacyKey;
        [SerializeField] private int index;

        internal string Scene => scene;
        internal int Index => index;
        private string Key => "StableField_v1_" + stableId;

        internal bool IsSpent(CharacterSceneData data) =>
            data.GetInt(Key, index >= 0 && index < data.toggleData.Count && data.toggleData[index] ? 1 : 0) != 0;

        internal void SetSpent(CharacterSceneData data, bool spent)
        {
            int value = spent ? 1 : 0;
            data.SetInt(Key, value);
            if (index >= 0 && index < data.toggleData.Count) data.toggleData[index] = spent;
            if (data.HasInt(legacyKey)) data.SetInt(legacyKey, value);
        }
    }

    [Serializable] private sealed class Map { public Field[] entries; }
    private static readonly Lazy<IReadOnlyList<Field>> Entries = new Lazy<IReadOnlyList<Field>>(Load);

    internal static IEnumerable<Field> ForScene(string name) => Entries.Value.Where(field => field.Scene == name);

    private static IReadOnlyList<Field> Load()
    {
        var asset = Resources.Load<TextAsset>("RestSpellSlotFields");
        if (asset == null) return Array.Empty<Field>();
        return Array.AsReadOnly((JsonUtility.FromJson<Map>(asset.text)?.entries ?? Array.Empty<Field>())
            .OrderBy(field => field.Index).ToArray());
    }
}
