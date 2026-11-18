using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Taruck.CharacterCreation
{
    // Relocalizes only untouched text produced by the creation wizard. Hand edits stay intact.
    internal static class CharacterCreationCardLocalization
    {
        private static readonly int[] NoteFields = { 78, 79, 80, 81, 102, 103, 104, 105 };

        internal static bool Apply(CharacterData character, CharacterSceneData sheet,
            IReadOnlyList<InputField> inputs, IReadOnlyList<TMP_InputField> tmpInputs, AppLanguage language)
        {
            if (character == null || sheet == null || !character.HasSharedString("CreationWizard_v1")) return false;
            CharacterCreationDraft draft;
            CreatedCharacterStats stats;
            try
            {
                draft = JsonUtility.FromJson<CharacterCreationDraft>(character.GetSharedString("CreationWizard_v1"));
                if (draft == null) return false;
                stats = CharacterCreationRules.Calculate(draft);
            }
            catch (Exception) { return false; }

            var catalog = new TranslationCatalog();
            string Read(int index) => CurrentValue(sheet, index, inputs, tmpInputs);
            void Write(int index, string value)
            {
                while (sheet.inputData.Count <= index) sheet.inputData.Add("");
                sheet.inputData[index] = value;
                string key = FindStableKey(index, inputs, tmpInputs);
                if (key != null) sheet.SetString(key, value);
            }
            bool changed = CreationArtificerResources.RestoreMissingBackgroundAttacks(character, draft, Read, Write,
                source => catalog.Translate(source, language));
            var variants = new Dictionary<AppLanguage, Dictionary<int, string>>();
            foreach (AppLanguage variant in new[] { AppLanguage.Ukrainian, AppLanguage.English, AppLanguage.Russian })
                variants[variant] = CharacterCreationPresentation.GeneratedSheetFields(draft, stats,
                    source => catalog.Translate(source, variant));
            bool notesUnedited = false;
            foreach (Dictionary<int, string> variant in variants.Values)
            {
                bool matches = true;
                foreach (int note in NoteFields)
                    if (CurrentValue(sheet, note, inputs, tmpInputs) != variant[note]) { matches = false; break; }
                if (matches) { notesUnedited = true; break; }
            }
            foreach (KeyValuePair<int, string> field in variants[language])
            {
                int index = field.Key;
                bool isNote = Array.IndexOf(NoteFields, index) >= 0;
                string stableKey = FindStableKey(index, inputs, tmpInputs);
                string current = CurrentValue(sheet, index, inputs, tmpInputs);
                if(index==75&&CreationAttackOverflow.Has(character))
                {
                    if(!CreationAttackOverflow.Refresh(character,current,variants,language,out string overflowText))continue;
                    if(current!=overflowText)
                    {
                        while(sheet.inputData.Count<=index)sheet.inputData.Add("");sheet.inputData[index]=overflowText;
                        if(stableKey!=null)sheet.SetString(stableKey,overflowText);
                        character.SetSharedString("CreationGeneratedText_v1_75",CreationDescriptionLayout.Fingerprint(overflowText));changed=true;
                    }
                    continue;
                }
                bool tracked = CreationDescriptionLayout.Tracks(index);
                string stampKey="CreationGeneratedText_v1_"+index;
                bool unchanged = tracked && character.GetSharedString(stampKey,"")==CreationDescriptionLayout.Fingerprint(current);
                if (isNote && !notesUnedited && !unchanged) continue;
                if (current == null) continue;
                if (current == field.Value)
                {
                    if(tracked&&!unchanged){character.SetSharedString(stampKey,CreationDescriptionLayout.Fingerprint(current));changed=true;}
                    continue;
                }
                bool generated = isNote && notesUnedited || unchanged;
                if (!generated)
                    foreach (Dictionary<int, string> variant in variants.Values)
                        if (current == variant[index]) { generated = true; break; }
                if (!generated) continue;
                while (sheet.inputData.Count <= index) sheet.inputData.Add("");
                sheet.inputData[index] = field.Value;
                if (stableKey != null) sheet.SetString(stableKey, field.Value);
                if(tracked)character.SetSharedString(stampKey,CreationDescriptionLayout.Fingerprint(field.Value));
                changed = true;
            }
            return changed;
        }

        private static string CurrentValue(CharacterSceneData sheet, int index,
            IReadOnlyList<InputField> inputs, IReadOnlyList<TMP_InputField> tmpInputs)
        {
            string key = FindStableKey(index, inputs, tmpInputs);
            return key != null && sheet.HasString(key) ? sheet.GetString(key)
                : index < sheet.inputData.Count ? sheet.inputData[index] : null;
        }

        private static string FindStableKey(int index, IReadOnlyList<InputField> inputs, IReadOnlyList<TMP_InputField> tmpInputs)
        {
            foreach (InputField input in inputs)
            {
                PersistentFieldId field = PersistentFieldId.For(input);
                if (field != null && field.LegacyCollection == "inputData" && field.LegacyIndex == index)
                    return "StableField_v1_" + field.Id;
            }
            foreach (TMP_InputField input in tmpInputs)
            {
                PersistentFieldId field = PersistentFieldId.For(input);
                if (field != null && field.LegacyCollection == "inputData" && field.LegacyIndex == index)
                    return "StableField_v1_" + field.Id;
            }
            return null;
        }
    }
}
