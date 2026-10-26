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
            var variants = new Dictionary<AppLanguage, Dictionary<int, string>>();
            foreach (AppLanguage variant in new[] { AppLanguage.Ukrainian, AppLanguage.English, AppLanguage.Russian })
                variants[variant] = CharacterCreationPresentation.GeneratedSheetFields(draft, stats,
                    source => catalog.Translate(source, variant));
            bool notesUnedited = false;
            foreach (Dictionary<int, string> variant in variants.Values)
            {
                bool matches = true;
                for (int note = 78; note <= 81; note++)
                    if (CurrentValue(sheet, note, inputs, tmpInputs) != variant[note]) { matches = false; break; }
                if (matches) { notesUnedited = true; break; }
            }
            bool changed = false;
            foreach (KeyValuePair<int, string> field in variants[language])
            {
                int index = field.Key;
                if (index >= 78 && index <= 81 && !notesUnedited) continue;
                string stableKey = FindStableKey(index, inputs, tmpInputs);
                string current = CurrentValue(sheet, index, inputs, tmpInputs);
                if (current == null || current == field.Value) continue;
                bool generated = index >= 78 && index <= 81 && notesUnedited;
                if (!generated)
                    foreach (Dictionary<int, string> variant in variants.Values)
                        if (current == variant[index]) { generated = true; break; }
                if (!generated) continue;
                while (sheet.inputData.Count <= index) sheet.inputData.Add("");
                sheet.inputData[index] = field.Value;
                if (stableKey != null) sheet.SetString(stableKey, field.Value);
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
