using System;

namespace Taruck.CharacterCreation
{
    internal static class CharacterSkillRules
    {
        internal static int[] Calculate(CharacterCreationDraft primary, CharacterCreationDraft secondary, CreatedCharacterStats stats)
        {
            int proficiency = CharacterLegacySpeciesRules.Proficiency(primary);
            int wisdom = CharacterCreationRules.Modifier(stats.scores[4]);
            var modifiers = new int[CharacterCreationRules.SkillAbilities.Count];
            for (int skill = 0; skill < modifiers.Length; skill++)
            {
                int ability = CharacterCreationRules.SkillAbilities[skill];
                int proficiencyBonus;
                if (stats.expertise?.Contains(skill) == true)
                    proficiencyBonus = 2 * proficiency;
                else if (stats.skills.Contains(skill))
                    proficiencyBonus = proficiency;
                else
                    proficiencyBonus = Math.Max(PartialProficiency(primary, stats, skill), PartialProficiency(secondary, stats, skill));

                modifiers[skill] = CharacterCreationRules.Modifier(stats.scores[ability]) + proficiencyBonus +
                    AbilityBonus(primary, skill, ability, wisdom) + AbilityBonus(secondary, skill, ability, wisdom);
            }
            return modifiers;
        }

        private static int PartialProficiency(CharacterCreationDraft track, CreatedCharacterStats stats, int skill) =>
            track == null ? 0 : Math.Max(CharacterBardRules.HalfProficiency(track), CharacterFighterRules.AthleteBonus(track, skill, stats.skills));

        private static int AbilityBonus(CharacterCreationDraft track, int skill, int ability, int wisdom)
        {
            if (track == null) return 0;
            if (track.edition == RulesEdition.Rules2024 &&
                ((track.characterClass == "druid" && track.primalOrder == "magician" && (skill == 2 || skill == 10)) ||
                 (track.characterClass == "cleric" && track.divineOrder == "thaumaturge" && (skill == 2 || skill == 14))))
                return Math.Max(1, wisdom);
            return ability == 5 && CharacterRangerRules.Is(track, "feywanderer") ? Math.Max(1, wisdom) : 0;
        }
    }
}
