using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    [Serializable]
    public sealed class CharacterClassHistory
    {
        [Serializable]
        public struct LevelEntry
        {
            public string classId;
            // The first entry uses the full die. Later entries store -1 for fixed HP or the rolled result.
            public int hitPoints;
        }

        public const int MaximumCharacterLevel = 20;
        public LevelEntry[] levels = Array.Empty<LevelEntry>();
        public int TotalLevel => levels?.Length ?? 0;

        internal int LevelOf(string classId) => levels?.Count(entry => entry.classId == classId) ?? 0;

        public (int die, int count)[] HitDice() => levels.GroupBy(entry => CharacterClassCatalog.Find(entry.classId).hitDie)
            .Select(group => (die: group.Key, count: group.Count())).ToArray();

        internal static CharacterClassHistory FromSingleClass(CharacterCreationDraft draft) => new CharacterClassHistory
        {
            levels = Enumerable.Range(0, Math.Clamp(draft.level, 0, MaximumCharacterLevel))
                .Select(i => new LevelEntry
                {
                    classId = draft.characterClass,
                    hitPoints = i == 0 ? 0 : draft.hitPointChoices?.ElementAtOrDefault(i - 1) ?? 0
                }).ToArray()
        };

        internal bool MatchesSingleClass(CharacterCreationDraft draft) => TotalLevel > 0 && TotalLevel == draft.level &&
            levels.SequenceEqual(FromSingleClass(draft).levels);

        public static CharacterClassHistory FromCreation(CharacterCreationDraft draft)
        {
            var history = FromSingleClass(draft);
            var secondary = draft.secondaryClass;
            if (secondary?.IsSelected == true)
                history.levels = history.levels.Concat(Enumerable.Range(0, secondary.level).Select(i => new LevelEntry
                {
                    classId = secondary.classId, hitPoints = secondary.hitPointChoices.ElementAtOrDefault(i)
                })).ToArray();
            return history;
        }

        public bool MatchesDraft(CharacterCreationDraft draft)
        {
            if (TotalLevel < 1 || TotalLevel != draft.TotalLevel || levels[0].classId != draft.characterClass) return false;
            var expected = FromCreation(draft).levels;
            return levels.OrderBy(entry => entry.classId, StringComparer.Ordinal)
                .SequenceEqual(expected.OrderBy(entry => entry.classId, StringComparer.Ordinal));
        }

        public bool CanTakeLevel(RulesEdition edition, string classId, IReadOnlyList<int> scores)
        {
            var nextClass = CharacterClassCatalog.Find(classId);
            if (TotalLevel < 1 || TotalLevel >= MaximumCharacterLevel || !Supports(nextClass, edition) ||
                levels.Any(entry => !Supports(CharacterClassCatalog.Find(entry.classId), edition))) return false;

            int currentLevel = LevelOf(classId);
            if (currentLevel >= nextClass.supportedLevel) return false;
            if (currentLevel > 0) return true;

            return scores != null && scores.Count == 6 &&
                levels.Select(entry => entry.classId).Append(classId).Distinct().All(id => MeetsRequirement(id, scores));
        }

        public bool TryAppend(RulesEdition edition, string classId, IReadOnlyList<int> scores, int hitPoints, out CharacterClassHistory next)
        {
            next = null;
            if (!CanTakeLevel(edition, classId, scores)) return false;
            int die = CharacterClassCatalog.Find(classId).hitDie;
            if (hitPoints != -1 && (hitPoints < 1 || hitPoints > die)) return false;
            next = new CharacterClassHistory { levels = levels.Append(new LevelEntry { classId = classId, hitPoints = hitPoints }).ToArray() };
            return true;
        }

        private static bool Supports(CharacterClassDefinition definition, RulesEdition edition) =>
            definition != null && definition.supportedLevel > 0 &&
            (edition == RulesEdition.Rules2014 || edition == RulesEdition.Rules2024 && definition.supports2024);

        private static bool MeetsRequirement(string classId, IReadOnlyList<int> scores)
        {
            bool StrengthOrDexterity() => scores[0] >= 13 || scores[1] >= 13;
            return classId switch
            {
                "barbarian" => scores[0] >= 13,
                "fighter" => StrengthOrDexterity(),
                "rogue" => scores[1] >= 13,
                "artificer" or "wizard" => scores[3] >= 13,
                "cleric" or "druid" => scores[4] >= 13,
                "bard" or "sorcerer" or "warlock" => scores[5] >= 13,
                "monk" or "ranger" => scores[1] >= 13 && scores[4] >= 13,
                "paladin" => scores[0] >= 13 && scores[5] >= 13,
                "bloodhunter" => StrengthOrDexterity() && scores[3] >= 13,
                _ => false
            };
        }
    }
}
