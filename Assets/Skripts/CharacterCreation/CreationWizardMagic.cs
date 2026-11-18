using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationWizardMagic
    {
        internal static string[] SavantOptions(CharacterCreationDraft d, int slot) =>
            Enumerable.Range(1, CharacterWizardRules.SavantCircle(slot))
                .SelectMany(circle => CharacterSpellCatalog.Available(d.edition, "wizard", circle))
                .Where(spell => spell.school == CharacterWizardRules.SchoolCode(d))
                .Select(spell => spell.name)
                .Except(d.spellbookSpells)
                .Except(d.wizardSavant.Where((_, i) => i != slot)).ToArray();

        internal static string[] BookOptions(CharacterCreationDraft d, int circle, bool mastery) =>
            CharacterWizardRules.Book(d).Select(name => CharacterSpellCatalog.Find(d.edition, name))
                .Where(spell => spell != null && spell.level == circle &&
                    (!mastery || !CharacterWizardRules.Modern(d) || spell.castingUnit == "action" && spell.castingAmount == 1))
                .Select(spell => spell.name).ToArray();

        internal static string ChoiceError(CharacterCreationDraft d)
        {
            if (!CharacterWizardRules.Active(d)) return null;
            if (d.wizardSavant.Where((name, i) => !SavantOptions(d, i).Contains(name)).Any())
                return CharacterWizardText.Get("savant.error");
            if (d.level >= 18 && (!BookOptions(d, 1, true).Contains(d.wizardMasteryOne) || !BookOptions(d, 2, true).Contains(d.wizardMasteryTwo)))
                return CharacterWizardText.Get("mastery.error");
            if (d.level >= 20 && d.wizardSignature.Any(name => !BookOptions(d, 3, false).Contains(name)))
                return CharacterWizardText.Get("signature.error");
            return null;
        }

        internal static string CardNote(CharacterCreationDraft d, string spell, Func<string, string> translate)
        {
            var notes = new List<string>();
            void Add(string key) => notes.Add(translate(CharacterWizardText.Get(key)));

            if (d.level >= 18 && (spell == d.wizardMasteryOne || spell == d.wizardMasteryTwo))
                Add(CharacterWizardRules.Modern(d) ? "mastery.card" : "mastery.card14");
            if (d.level >= 20 && d.wizardSignature.Contains(spell)) Add("signature.card");
            if (CharacterWizardRules.Is(d, "necromancy", 6) && spell == "Animate Dead") Add("necromancy.6");
            if (CharacterWizardRules.Is(d, "transmutation", 10) && spell == "Polymorph") Add("transmutation.10");
            if (CharacterWizardRules.Is(d, "illusion") && spell == "Minor Illusion")
                Add(CharacterWizardRules.Modern(d) ? "illusion.first24" : "illusion.first14");
            if (CharacterWizardRules.Modern(d) && CharacterWizardRules.Is(d, "illusion", 6) &&
                (spell == "Summon Beast" || spell == "Summon Fey")) Add("illusion.6.24");
            if (CharacterWizardRules.Modern(d) && CharacterWizardRules.Is(d, "abjuration", 10) &&
                (spell == "Counterspell" || spell == "Dispel Magic")) Add("abjuration.10.24");
            return string.Join("\n", notes);
        }

        internal static void WriteResources(CharacterData character, CreationSheetBindings bindings, CharacterCreationDraft d, Func<string, string> translate)
        {
            void Pool(string key, int count, bool shortRest) =>
                CreationMagicResources.Add(character, bindings, translate(CharacterWizardText.Get(key)), count, shortRest);

            Pool("recovery.pool", 1, false);
            if (d.level >= 20)
                for (int i = 0; i < d.wizardSignature.Count; i++)
                    CreationMagicResources.Add(character, bindings,
                        string.Format(translate(CharacterWizardText.Get("signature.pool")), i + 1), 1, true);
            if (CharacterWizardRules.Is(d, "divination"))
            {
                Pool("portent", d.level >= 14 ? 3 : 2, false);
                if (d.level >= 10) Pool("third.eye", 1, true);
            }
            if (CharacterWizardRules.Is(d, "illusion", 10)) Pool("illusory.self", 1, true);
            if (CharacterWizardRules.Is(d, "evocation", 14)) Pool("overchannel", 1, false);
            if (CharacterWizardRules.Is(d, "conjuration", 6)) Pool("benign", 1, false);
            if (CharacterWizardRules.Is(d, "transmutation", 10)) Pool("shapechanger", 1, true);
            if (CharacterWizardRules.Modern(d) && CharacterWizardRules.Is(d, "illusion", 6))
            {
                CreationMagicResources.Add(character, bindings, translate("Summon Beast"), 1, false);
                CreationMagicResources.Add(character, bindings, translate("Summon Fey"), 1, false);
            }
        }
    }
}
