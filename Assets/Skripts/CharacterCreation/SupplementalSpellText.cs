using System;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    internal sealed class SupplementalSpellText
    {
        [Serializable] private sealed class Entry { public string name, classic, revised; }
        [Serializable] private sealed class Catalog { public Entry[] entries; }

        private readonly Lazy<Entry[]> entries;

        internal SupplementalSpellText(string resource)
        {
            entries = new Lazy<Entry[]>(() =>
            {
                var asset = Resources.Load<TextAsset>("CharacterCreation/" + resource);
                return asset == null ? Array.Empty<Entry>() :
                    JsonUtility.FromJson<Catalog>(asset.text)?.entries ?? Array.Empty<Entry>();
            });
        }

        internal string Effect(RulesEdition edition, string name)
        {
            foreach (var entry in entries.Value)
                if (entry.name == name) return edition == RulesEdition.Rules2024 ? entry.revised : entry.classic;
            return "";
        }
    }
}
