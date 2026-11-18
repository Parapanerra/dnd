using System;
using System.Linq;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    internal static class CharacterTricksterSpells
    {
        private static readonly Dictionary<string, bool> cache = new Dictionary<string, bool>();
        private static readonly int[] LearnedAt = { 3, 3, 3, 4, 7, 8, 10, 11, 13, 14, 16, 19, 20 };
        internal static bool Active(CharacterCreationDraft d) => CharacterRogueRules.Is(d, "trickster");
        internal static bool ThirdCaster(CharacterCreationDraft d) => Active(d) || CharacterFighterRules.Caster(d);
        private static bool PreferredSchool(CharacterCreationDraft d, CharacterSpellRecord spell) => CharacterFighterRules.Caster(d) ? spell.school == "A" || spell.school == "V" : spell.school == "E" || spell.school == "I";
        internal static int MaximumCircle(int level) => level >= 19 ? 4 : level >= 13 ? 3 : level >= 7 ? 2 : level >= 3 ? 1 : 0;
        internal static CharacterSpellRecord[] Available(CharacterCreationDraft d, int circle) => circle == 0
            ? CharacterSpellCatalog.Available(d.edition, "wizard", 0).Where(s => !Active(d) || s.name != "Mage Hand").ToArray()
            : Enumerable.Range(1, MaximumCircle(d.level)).SelectMany(level => CharacterSpellCatalog.Available(d.edition, "wizard", level)).ToArray();
        // Match the final repertoire to the actual learning slots. A higher-circle replacement
        // consumes one level-up replacement; original unrestricted-school slots retain their status.
        internal static bool CanLearn(CharacterCreationDraft d, IEnumerable<string> names)
        {
            if (!ThirdCaster(d)) return true;
            var list = names.Where(name => !string.IsNullOrEmpty(name)).ToArray();
            if (list.Distinct().Count() != list.Length) return false;
            var pool = Available(d, 1); var spells = list.Select(name => pool.FirstOrDefault(s => s.name == name)).ToArray();
            if (spells.Any(s => s == null)) return false;
            string key = d.characterClass + ":" + d.edition + ":" + d.level + ":" + string.Join("|", spells.GroupBy(spell => spell.level + ":" + (d.edition == RulesEdition.Rules2024 || PreferredSchool(d, spell) ? "school" : "other")).OrderBy(group => group.Key).Select(group => group.Key + "=" + group.Count()));
            if (cache.TryGetValue(key, out bool cached)) return cached;
            if (cache.Count > 4096) cache.Clear();
            var slots = LearnedAt.Select((at, index) => new { at, unrestricted = d.edition == RulesEdition.Rules2024 || index == 2 || index == 5 || index == 9 || index == 12 }).Where(s => s.at <= d.level).ToArray();
            if (spells.Length > slots.Length) return false;
            spells = spells.OrderByDescending(s => !PreferredSchool(d, s)).ThenByDescending(s => s.level).ToArray();
            var failed = new HashSet<long>();
            bool Search(int index, int used, int r2 = 0, int r3 = 0, int r4 = 0)
            {
                if (r4 > Math.Max(0, d.level - 18) || r4 + r3 > Math.Max(0, d.level - 12) || r4 + r3 + r2 > Math.Max(0, d.level - 6)) return false;
                if (index == spells.Length) return true;
                long state = used | ((long)r2 << 13) | ((long)r3 << 17) | ((long)r4 << 21);
                if (failed.Contains(state)) return false;
                var spell = spells[index]; var tried = new HashSet<string>();
                for (int i = slots.Length - 1; i >= 0; i--)
                {
                    if ((used & (1 << i)) != 0 || !slots[i].unrestricted && !PreferredSchool(d, spell)) continue;
                    if (!tried.Add(slots[i].at + ":" + slots[i].unrestricted)) continue;
                    bool replacement = spell.level > MaximumCircle(slots[i].at);
                    if (Search(index + 1, used | (1 << i), r2 + (replacement && spell.level == 2 ? 1 : 0), r3 + (replacement && spell.level == 3 ? 1 : 0), r4 + (replacement && spell.level == 4 ? 1 : 0))) return true;
                }
                failed.Add(state); return false;
            }
            bool result = Search(0, 0); cache[key] = result; return result;
        }
        internal static string[] Options(CharacterCreationDraft d, IList<string> chosen, int slot) => Available(d, 1)
            .Where(spell => (spell.name == chosen[slot] || !chosen.Contains(spell.name)) && CanLearn(d, chosen.Where((_, index) => index != slot).Concat(new[] { spell.name }))).Select(spell => spell.name).ToArray();
    }
}
