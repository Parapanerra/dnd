using System;
using System.Linq;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    public static class CharacterWizardRules
    {
        private static readonly int[] RevisedPrepared = { 4, 5, 6, 7, 9, 10, 11, 12, 14, 15, 16, 16, 17, 18, 19, 21, 22, 23, 24, 25 };

        internal static int Cantrips(CharacterCreationDraft draft) => draft.level >= 10 ? 5 : draft.level >= 4 ? 4 : 3;
        internal static int Prepared(CharacterCreationDraft draft) => draft.edition == RulesEdition.Rules2024
            ? RevisedPrepared[Math.Clamp(draft.level, 1, 20) - 1]
            : Math.Max(1, draft.level + CharacterCreationRules.Modifier(CharacterCreationRules.Scores(draft)[3]));
        public static int BookCount(CharacterCreationDraft draft) => 6 + 2 * (Math.Clamp(draft.level, 1, 20) - 1);
        public static int BookCircle(int index)
        {
            int learnedAt = index < 6 ? 1 : 2 + (index - 6) / 2;
            return Math.Min(9, (learnedAt + 1) / 2);
        }
        internal static int Slots(CharacterCreationDraft draft, int circle) => CharacterFullCasterSlots.Count(draft.level, circle);

        private static readonly string[] ClassicSchools = { "abjuration", "conjuration", "divination", "enchantment", "evocation", "illusion", "necromancy", "transmutation" };
        private static readonly string[] RevisedSchools = { "abjuration", "divination", "evocation", "illusion" };

        public static bool Active(CharacterCreationDraft d) => d.characterClass == "wizard";
        public static bool Modern(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static int SchoolLevel(CharacterCreationDraft d) => Modern(d) ? 3 : 2;
        public static string[] Schools(CharacterCreationDraft d) => Modern(d) ? RevisedSchools : ClassicSchools;
        public static bool HasSchool(CharacterCreationDraft d) => Active(d) && d.level >= SchoolLevel(d) && Schools(d).Contains(d.subclass);
        public static bool Is(CharacterCreationDraft d, string school, int level = 0) => HasSchool(d) && d.subclass == school && d.level >= level;
        public static string SchoolCode(CharacterCreationDraft d) => d.subclass switch
        {
            "abjuration" => "A", "divination" => "D", "evocation" => "V", "illusion" => "I", _ => ""
        };
        public static int SavantCount(CharacterCreationDraft d) => Modern(d) && HasSchool(d) ? Math.Min(9, (d.level + 1) / 2) : 0;
        public static int SavantCircle(int index) => Math.Max(2, index + 1);
        public static IEnumerable<string> Book(CharacterCreationDraft d) => d.spellbookSpells.Concat(d.wizardSavant)
            .Concat(Is(d, "necromancy", 6) ? new[] { "Animate Dead" } : Array.Empty<string>())
            .Concat(Is(d, "transmutation", 10) ? new[] { "Polymorph" } : Array.Empty<string>())
            .Where(name => !string.IsNullOrEmpty(name)).Distinct();
        public static IEnumerable<string> Automatic(CharacterCreationDraft d)
        {
            if (!Active(d)) yield break;
            if (Is(d, "illusion")) yield return "Minor Illusion";
            if (Modern(d) && Is(d, "abjuration", 10)) { yield return "Counterspell"; yield return "Dispel Magic"; }
            if (Modern(d) && Is(d, "illusion", 6)) { yield return "Summon Beast"; yield return "Summon Fey"; }
            if (d.level >= 18 && Modern(d))
            {
                if (d.wizardMasteryOne.Length > 0) yield return d.wizardMasteryOne;
                if (d.wizardMasteryTwo.Length > 0) yield return d.wizardMasteryTwo;
            }
            if (d.level >= 20)
                foreach (string spell in d.wizardSignature.Where(name => !string.IsNullOrEmpty(name))) yield return spell;
        }
        public static int[] ScholarOptions(CharacterCreationDraft d) => CharacterCreationRules.BackgroundSkills(d)
            .Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d))
            .Where(skill => new[] { 2, 5, 8, 9, 10, 14 }.Contains(skill)).Distinct().OrderBy(skill => skill).ToArray();
        internal static int RecoveryBudget(CharacterCreationDraft d) => Math.Max(1, (d.level + 1) / 2);

        public static string ChoiceError(CharacterCreationDraft d)
        {
            if (!Active(d)) return null;
            if (d.level >= SchoolLevel(d) && !HasSchool(d)) return CharacterWizardText.Get("school.error");
            if (Modern(d) && d.level >= 2 && !ScholarOptions(d).Contains(d.wizardScholar) &&
                (!d.IsSecondaryTrack || ScholarOptions(d).Length > 0)) return CharacterWizardText.Get("scholar.error");
            if (d.wizardSavant.Count != SavantCount(d) || d.wizardSavant.Any(string.IsNullOrEmpty) ||
                d.wizardSavant.Concat(d.spellbookSpells).Distinct().Count() != d.wizardSavant.Count + d.spellbookSpells.Count)
                return CharacterWizardText.Get("savant.error");
            if (d.level >= 18 && (!Book(d).Contains(d.wizardMasteryOne) || !Book(d).Contains(d.wizardMasteryTwo) || d.wizardMasteryOne == d.wizardMasteryTwo))
                return CharacterWizardText.Get("mastery.error");
            if (d.level >= 20 && (d.wizardSignature.Count != 2 || d.wizardSignature.Distinct().Count() != 2 || d.wizardSignature.Any(name => !Book(d).Contains(name))))
                return CharacterWizardText.Get("signature.error");
            return null;
        }

        internal static void Repair(CharacterCreationDraft d)
        {
            d.wizardSavant ??= new List<string>();
            d.wizardSignature ??= new List<string>();
            if (d.wizardSavant.Count > SavantCount(d)) d.wizardSavant.RemoveRange(SavantCount(d), d.wizardSavant.Count - SavantCount(d));
            if (!Active(d) || !Modern(d) || d.level < 2) d.wizardScholar = -1;
            if (!Active(d) || d.level < 18 || !Book(d).Contains(d.wizardMasteryOne)) d.wizardMasteryOne = "";
            if (!Active(d) || d.level < 18 || !Book(d).Contains(d.wizardMasteryTwo)) d.wizardMasteryTwo = "";
            if (!Active(d) || d.level < 20) d.wizardSignature.Clear();
            else for (int i = 0; i < d.wizardSignature.Count; i++)
                if (!Book(d).Contains(d.wizardSignature[i])) d.wizardSignature[i] = "";
            if (!Active(d)) return;
            if (d.spellbookSpells.Count > BookCount(d)) d.spellbookSpells.RemoveRange(BookCount(d), d.spellbookSpells.Count - BookCount(d));
            if (Is(d, "illusion"))
                for (int i = 0; i < d.cantrips.Count; i++) if (d.cantrips[i] == "Minor Illusion") d.cantrips[i] = "";
        }

        private static IEnumerable<string> Features(CharacterCreationDraft d, int intelligenceModifier)
        {
            string Text(string key) => CharacterWizardText.Get(key);
            yield return string.Format(Text(Modern(d) ? "recovery24" : "recovery"), RecoveryBudget(d));
            yield return Text("ritual");
            if (Modern(d) && d.level >= 2) yield return Text("scholar.note");
            if (Modern(d) && d.level >= 5) yield return Text("memorize");
            if (d.level >= 18) yield return Text("mastery.note");
            if (d.level >= 20) yield return Text("signature.note");
            if (!HasSchool(d)) yield break;
            bool modern = Modern(d);
            yield return Text(modern ? "savant24" : "savant14");
            if (Is(d, "abjuration"))
            {
                yield return string.Format(Text("ward.note"), Math.Max(1, 2 * d.level + intelligenceModifier));
                if (modern) yield return Text("ward.24");
            }
            else if (Is(d, "divination"))
                yield return string.Format(Text("divination.first"), d.level >= 14 ? 3 : 2);
            else if (Is(d, "evocation"))
                yield return Text(modern ? "evocation.potent24" : "evocation.sculpt");
            else if (Is(d, "illusion"))
                yield return Text(modern ? "illusion.first24" : "illusion.first14");
            else yield return Text(d.subclass + ".first");

            if (d.level >= 6)
                yield return Text(d.subclass switch
                {
                    "evocation" => modern ? "evocation.sculpt" : "evocation.potent14",
                    "illusion" => modern ? "illusion.6.24" : "illusion.6.14",
                    _ => d.subclass + ".6"
                });
            if (d.level >= 10)
                yield return Text(d.subclass + ".10" + (new[] { "illusion", "abjuration", "divination" }.Contains(d.subclass) ? (modern ? ".24" : ".14") : ""));
            if (d.level >= 14 && d.subclass != "divination") yield return Text(d.subclass + ".14");
        }

        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d);
            bool modern = d.edition == RulesEdition.Rules2024, dwarf = CharacterSpeciesRules.IsDwarf(d);
            int Mod(int ability) => CharacterCreationRules.Modifier(scores[ability]);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if (modern && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            var result = new CreatedCharacterStats {
                scores = scores, skills = skills.Distinct().ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, Mod(2)), armorClass = 10 + Mod(1),
                initiative = Mod(1) + 0,
                speed = dwarf && !modern ? 25 : 30,
                passivePerception = 10 + Mod(4) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = dwarf ? modern ? "Дварф" : "Дварф (пагорбовий)" : "Людина",
                backgroundName = CharacterBackgroundRules.Find(d)?.name ?? ""
            };
            if (modern && d.level >= 2 && ScholarOptions(d).Contains(d.wizardScholar))
                result.expertise = new List<int> { d.wizardScholar };
            result.features = string.Join("\n", Features(d, Mod(3))) + CharacterBackgroundRules.Features(d);
            result.equipment = Equipment(d);
            return result;
        }

        internal static string Equipment(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024
            ? d.equipment == 0 ? "2 кинджали; арканний фокус (посох); мантія; книга заклинань; набір ученого; 5 зм." : "55 зм замість класового спорядження."
            : d.wizardWeapon + "; " + d.wizardFocus + "; книга заклинань; " + (d.wizardPack == 0 ? "набір ученого." : "набір мандрівника.");
    }
}
