using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    [Serializable]
    public sealed class CharacterSecondaryClass
    {
        public const int MaximumLevel = CharacterClassHistory.MaximumCharacterLevel - 1;
        public string classId = "";
        public int level = 1;
        public int[] hitPointChoices = new int[MaximumLevel];
        public FighterStyle fightingStyle = (FighterStyle)(-1);
        public List<int> masteries = new List<int>();
        public CharacterClassChoices choices;
        public bool IsSelected => !string.IsNullOrEmpty(classId);

        public static IEnumerable<CharacterClassDefinition> Options(CharacterCreationDraft primary) =>
            CharacterClassCatalog.SelectableFor(primary.edition).Where(definition =>
                definition.id != primary.characterClass);

        public void ResizeHitPointChoices()
        {
            if (hitPointChoices?.Length != MaximumLevel) Array.Resize(ref hitPointChoices, MaximumLevel);
        }

        public CharacterCreationDraft ClassDraft(RulesEdition edition)
        {
            var track = new CharacterCreationDraft { secondaryClass = null };
            choices?.CopyChoicesTo(track);
            track.characterClass = classId;
            track.edition = edition;
            track.level = level;
            track.fightingStyle = fightingStyle;
            track.weaponMasteries = masteries;
            return track;
        }

        public CharacterCreationDraft ClassDraft(CharacterCreationDraft primary)
        {
            var track = ClassDraft(primary.edition);
            track.multiclassExistingSkills = CharacterCreationRules.BackgroundSkills(primary).Concat(primary.classSkills)
                .Concat(CharacterSpeciesRules.GrantedSkills(primary)).Concat(CharacterOriginFeatRules.Skills(primary))
                .Concat(CharacterBardRules.ExtraSkills(primary)).Concat(CharacterRangerRules.FeySkills(primary))
                .Concat(primary.humanSkill >= 0 ? new[] { primary.humanSkill } : Array.Empty<int>())
                .Distinct().ToArray();
            track.multiclassExistingLanguages = CharacterSpeciesRules.AutomaticLanguages(primary)
                .Concat(CharacterSpeciesRules.AdditionalLanguages(primary)).Distinct().ToArray();
            track.multiclassScores = CharacterCreationRules.Scores(primary);
            track.multiclassTotalLevel = primary.TotalLevel;
            track.multiclassOwner = primary;
            return track;
        }

        internal CharacterCreationDraft EquipmentTrack(CharacterCreationDraft primary)
        {
            var track = ClassDraft(primary.edition);
            track.multiclassOwner = primary;
            track.multiclassExistingSkills = Array.Empty<int>();
            track.multiclassTotalLevel = primary.TotalLevel;
            return track;
        }

        internal static int EntrySkills(CharacterCreationDraft track) =>
            track.characterClass == "bard" || track.characterClass == "rogue" || track.characterClass == "ranger" ||
            track.characterClass == "artificer" && track.edition == RulesEdition.Rules2024 ? 1 : 0;

        public void SaveChoices(CharacterCreationDraft track)
        {
            fightingStyle = track.fightingStyle;
            masteries = track.weaponMasteries;
            choices ??= new CharacterClassChoices();
            track.CopyChoicesTo(choices);
        }

        internal int[] BonusSkills(CharacterCreationDraft track) => new[]
        {
            CharacterBarbarianRules.Modern(track) && CharacterBarbarianRules.Active(track) && track.level >= 3 ? track.primalKnowledgeSkill : -1,
            CharacterFighterRules.Revised(track) && CharacterFighterRules.Is(track, "battlemaster") ? track.fighterSkill : -1
        }.Where(skill => skill >= 0).Concat(track.classSkills).ToArray();

        internal static bool GrantsWeapon(CharacterCreationDraft track, CoreWeapon weapon) => track.characterClass switch
        {
            "fighter" or "barbarian" or "paladin" or "ranger" => track.edition == RulesEdition.Rules2014 || !weapon.simple,
            "bloodhunter" => true,
            "monk" => track.edition == RulesEdition.Rules2014 && (weapon.simple || weapon.name == "Короткий меч"),
            "cleric" => CharacterClericRules.MartialWeapons(track),
            "druid" => track.edition == RulesEdition.Rules2024 && track.primalOrder == "warden",
            "warlock" => track.edition == RulesEdition.Rules2014 && weapon.simple,
            "bard" => CharacterBardRules.Is(track, "valor"),
            _ => false
        };

        public bool PreservesChoices(CharacterSecondaryClass previous, RulesEdition edition)
        {
            if (classId != previous.classId || fightingStyle != previous.fightingStyle ||
                !masteries.Take(previous.masteries.Count).SequenceEqual(previous.masteries))
                return false;
            var before = previous.ClassDraft(edition);
            var after = ClassDraft(edition);
            CharacterLevelAdvancement.EnsureArrays(before);
            CharacterLevelAdvancement.EnsureArrays(after);
            int advancementCount = CharacterLevelAdvancement.Count(before);
            return (string.IsNullOrEmpty(before.subclass) || before.subclass == after.subclass) &&
                after.advancementModes.Take(advancementCount).SequenceEqual(before.advancementModes.Take(advancementCount)) &&
                after.advancementFirst.Take(advancementCount).SequenceEqual(before.advancementFirst.Take(advancementCount)) &&
                after.advancementSecond.Take(advancementCount).SequenceEqual(before.advancementSecond.Take(advancementCount)) &&
                after.classSkills.SequenceEqual(before.classSkills) &&
                after.fighterManeuvers.Take(before.fighterManeuvers.Count).SequenceEqual(before.fighterManeuvers) &&
                after.preparedSpells.Take(before.preparedSpells.Count).SequenceEqual(before.preparedSpells) &&
                after.cantrips.Take(before.cantrips.Count).SequenceEqual(before.cantrips);
        }

        public string ChoiceError(CharacterCreationDraft primary, bool checkRequirements)
        {
            var definition = Options(primary).FirstOrDefault(option => option.id == classId);
            if (definition == null || level < 1 || level > MaximumLevel || primary.TotalLevel > CharacterClassHistory.MaximumCharacterLevel)
                return "Обери допустимий клас і рівень мультикласу.";
            if (checkRequirements)
            {
                var entry = new CharacterCreationDraft { characterClass = primary.characterClass, edition = primary.edition, level = primary.level };
                primary.CopyChoicesTo(entry);
                if (!CharacterClassHistory.FromSingleClass(entry).CanTakeLevel(primary.edition, classId,
                        CharacterCreationRules.Scores(entry, includeEquipment: false)))
                    return "Характеристики не відповідають вимогам мультикласу.";
            }

            if (hitPointChoices == null || hitPointChoices.Length < level || hitPointChoices.Take(level).Any(choice =>
                    choice != -1 && (choice < 1 || choice > definition.hitDie))) return "Обери ОЗ для кожного рівня другого класу.";

            var track = ClassDraft(primary);
            track.hitPointChoices = hitPointChoices.Skip(1).ToArray();
            if (CharacterSpeciesRules.AdditionalLanguages(track).Any(track.multiclassExistingLanguages.Contains))
                return "Обери додаткові мови без повторів уже відомих.";
            return CharacterCreationRules.Validate(track, requireName: false).FirstOrDefault() ??
                CharacterLevelAdvancement.ChoiceError(primary, track);
        }
    }
}
