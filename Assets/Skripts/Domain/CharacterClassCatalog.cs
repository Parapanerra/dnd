using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public sealed class CharacterClassDefinition
    {
        public readonly string id;
        public readonly string name;
        public readonly int hitDie;
        public readonly IReadOnlyList<int> savingThrows;
        public readonly int skillCount;
        private readonly IReadOnlyList<int> skills2014;
        private readonly IReadOnlyList<int> skills2024;
        public readonly int supportedLevel;
        public readonly bool supports2024;

        internal CharacterClassDefinition(string id, string name, int hitDie, int[] savingThrows,
            int skillCount, int[] skills2014, int[] skills2024 = null, int supportedLevel = 0, bool supports2024 = true)
        {
            this.id = id;
            this.name = name;
            this.hitDie = hitDie;
            this.savingThrows = Array.AsReadOnly((int[])savingThrows.Clone());
            this.skillCount = skillCount;
            this.skills2014 = Array.AsReadOnly((int[])skills2014.Clone());
            this.skills2024 = skills2024 == null ? this.skills2014 : Array.AsReadOnly((int[])skills2024.Clone());
            this.supportedLevel = supportedLevel;
            this.supports2024 = supports2024;
        }

        internal IReadOnlyList<int> Skills(RulesEdition edition) => edition == RulesEdition.Rules2024 ? skills2024 : skills2014;
    }

    // Source metadata for the thirteen classes in the pinned PHB/XPHB and TCE/EFA snapshot.
    // A class becomes selectable only when its rules and sheet transfer are implemented.
    public static class CharacterClassCatalog
    {
        private static readonly CharacterClassDefinition[] Definitions =
        {
            new CharacterClassDefinition("artificer", "Винахідник", 8, new[] { 2, 3 }, 2, new[] { 2, 5, 8, 9, 10, 11, 15 }, supportedLevel: 20),
            new CharacterClassDefinition("barbarian", "Варвар", 12, new[] { 0, 2 }, 2, new[] { 1, 3, 7, 10, 11, 17 }, supportedLevel: 20),
            new CharacterClassDefinition("bloodhunter", "Мисливець крові", 10, new[] { 1, 3 }, 3, new[] { 0, 3, 17, 5, 2, 6, 8, 14 }, supportedLevel: 20, supports2024: false),
            new CharacterClassDefinition("bard", "Бард", 8, new[] { 1, 5 }, 3, Enumerable.Range(0, 18).ToArray(), supportedLevel: 20),
            new CharacterClassDefinition("cleric", "Жрець", 8, new[] { 4, 5 }, 2, new[] { 5, 6, 9, 13, 14 }, supportedLevel: 20),
            new CharacterClassDefinition("druid", "Друїд", 8, new[] { 3, 4 }, 2, new[] { 1, 2, 6, 9, 10, 11, 14, 17 }, supportedLevel: 20),
            new CharacterClassDefinition("fighter", "Воїн", 10, new[] { 0, 2 }, 2,
                new[] { 0, 1, 3, 5, 6, 7, 11, 17 }, new[] { 0, 1, 3, 5, 6, 7, 11, 13, 17 }, 20),
            new CharacterClassDefinition("monk", "Монах", 8, new[] { 0, 1 }, 2, new[] { 0, 3, 5, 6, 14, 16 }, supportedLevel: 20),
            new CharacterClassDefinition("paladin", "Паладин", 10, new[] { 4, 5 }, 2, new[] { 3, 6, 7, 9, 13, 14 }, supportedLevel: 20),
            new CharacterClassDefinition("ranger", "Слідопит", 10, new[] { 0, 1 }, 3, new[] { 1, 3, 6, 8, 10, 11, 16, 17 }, supportedLevel: 20, supports2024: true),
            new CharacterClassDefinition("rogue", "Плут", 8, new[] { 1, 3 }, 4,
                new[] { 0, 3, 4, 6, 7, 8, 11, 12, 13, 15, 16 },
                new[] { 0, 3, 4, 6, 7, 8, 11, 13, 15, 16 }, 20),
            new CharacterClassDefinition("sorcerer", "Чародій", 6, new[] { 2, 5 }, 2, new[] { 2, 4, 6, 7, 13, 14 }, supportedLevel: 20),
            new CharacterClassDefinition("warlock", "Чаклун", 8, new[] { 4, 5 }, 2, new[] { 2, 4, 5, 7, 8, 10, 14 }, supportedLevel: 20),
            new CharacterClassDefinition("wizard", "Чарівник", 6, new[] { 3, 4 }, 2,
                new[] { 2, 5, 6, 8, 9, 14 }, new[] { 2, 5, 6, 8, 9, 10, 14 }, supportedLevel: 20)
        };

        private static readonly Dictionary<string, CharacterClassDefinition> ById =
            Definitions.ToDictionary(definition => definition.id, StringComparer.Ordinal);

        internal static IEnumerable<CharacterClassDefinition> Selectable => Definitions.Where(definition => definition.supportedLevel > 0);
        public static IEnumerable<CharacterClassDefinition> SelectableFor(RulesEdition edition) =>
            Selectable.Where(definition => edition != RulesEdition.Rules2024 || definition.supports2024);
        public static CharacterClassDefinition Find(string id) =>
            id != null && ById.TryGetValue(id, out CharacterClassDefinition definition) ? definition : null;
    }
}
