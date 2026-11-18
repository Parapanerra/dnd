using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private void DrawWizardChoices()
        {
            if (CharacterWizardRules.Modern(draft) && draft.level >= 2)
                DropdownInt(CharacterWizardText.Get("scholar"), CharacterWizardRules.ScholarOptions(draft), draft.wizardScholar,
                    value => draft.wizardScholar = value, skill => CharacterCreationRules.SkillNames[skill], 290);

            int count = CharacterWizardRules.SavantCount(draft);
            while (draft.wizardSavant.Count < count) draft.wizardSavant.Add("");
            RectTransform row = null;
            for (int i = 0; i < count; i++)
            {
                int slot = i;
                string[] options = CreationWizardMagic.SavantOptions(draft, slot);
                DropdownKey(LF(CharacterWizardText.Get("savant.pick"), CharacterWizardRules.SavantCircle(slot)),
                    options, options.Select(L).ToArray(), draft.wizardSavant[slot], value => draft.wizardSavant[slot] = value,
                    320, SpellChoiceHost(i, ref row));
            }

            if (draft.level >= 18)
            {
                string[] first = CreationWizardMagic.BookOptions(draft, 1, true);
                string[] second = CreationWizardMagic.BookOptions(draft, 2, true);
                DropdownKey(LF(CharacterWizardText.Get("mastery.pick"), 1), first, first.Select(L).ToArray(),
                    draft.wizardMasteryOne, value => draft.wizardMasteryOne = value, 320);
                DropdownKey(LF(CharacterWizardText.Get("mastery.pick"), 2), second, second.Select(L).ToArray(),
                    draft.wizardMasteryTwo, value => draft.wizardMasteryTwo = value, 320);
            }

            if (draft.level < 20) return;
            while (draft.wizardSignature.Count < 2) draft.wizardSignature.Add("");
            for (int i = 0; i < 2; i++)
            {
                int slot = i;
                string[] options = CreationWizardMagic.BookOptions(draft, 3, false).Where(name => name != draft.wizardSignature[1 - slot]).ToArray();
                DropdownKey(LF(CharacterWizardText.Get("signature.pick"), i + 1), options, options.Select(L).ToArray(),
                    draft.wizardSignature[i], value => draft.wizardSignature[slot] = value, 320);
            }
        }
    }
}
