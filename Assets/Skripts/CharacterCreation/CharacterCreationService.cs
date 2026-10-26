using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    // Builds a complete candidate before writing through the existing atomic repository.
    public sealed class CharacterCreationService
    {
        private readonly CreationSheetBindings bindings;
        public CharacterCreationService(CreationSheetBindings bindings) { this.bindings = bindings; }

        public CharacterData Build(CharacterCreationDraft draft)
        {
            var errors = CharacterCreationRules.Validate(draft);
            if (errors.Count > 0) throw new ArgumentException(string.Join("\n", errors));
            if (bindings == null || bindings.entries.Length < 30 || string.IsNullOrEmpty(bindings.healthPrefix))
                throw new InvalidOperationException("Не налаштовано зв’язок майстра з карткою персонажа.");
            CreatedCharacterStats s = CharacterCreationRules.Calculate(draft);
            // Sheet inputs are editable user-facing text. Generate their initial value in
            // the selected language, while the saved draft retains language-neutral choices.
            RuntimeLocalization localization = new RuntimeManagerResolver().Localization();
            Func<string, string> translate = localization.Translate;
            Dictionary<int, string> generated = CharacterCreationPresentation.GeneratedSheetFields(draft, s, translate);
            var c = new CharacterData(Guid.NewGuid().ToString()) { characterName = draft.characterName.Trim(), currentHealth = s.hitPoints, maxHealth = s.hitPoints };
            c.SetSharedString("CreationWizard_v1", JsonUtility.ToJson(draft));
            c.SetSharedString("RulesEdition", draft.edition == RulesEdition.Rules2024 ? "2024" : "2014");
            void Write(int index, string value) => bindings.Text(c, "input." + index, value);
            Write(57, c.characterName); Write(58, "1"); Write(59, "0");
            foreach (int index in new[] { 60, 62, 63, 64, 65 }) Write(index, generated[index]);
            for (int i = 0; i < 6; i++)
            {
                Write(27 + 2 * i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i])));
                Write(28 + 2 * i, s.scores[i].ToString());
                bindings.Text(c, "save." + i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i]) + (i == 0 || i == 2 ? 2 : 0)));
                bindings.Toggle(c, "save." + i, i == 0 || i == 2);
            }
            for (int i = 0; i < 18; i++)
            {
                bindings.Text(c, "skill." + i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[CharacterCreationRules.SkillAbilities[i]]) + (s.skills.Contains(i) ? 2 : 0)));
                bindings.Toggle(c, "skill." + i, s.skills.Contains(i));
            }
            Write(24, CharacterCreationRules.Signed(s.initiative)); Write(25, s.speed.ToString()); Write(26, s.armorClass.ToString());
            Write(39, "+2"); Write(40, s.passivePerception.ToString()); Write(42, "1"); Write(43, "10");
            for (int i = 49; i <= 53; i++) Write(i, "0");
            int classGold = draft.edition == RulesEdition.Rules2024 ? draft.equipment == 0 ? 4 : 11 : 0;
            int originGold = draft.edition == RulesEdition.Rules2024
                ? draft.backgroundEquipmentChoice == 1 ? 50 : draft.background == "soldier" ? 14 : 16
                : draft.background == "soldier" ? 10 : 15;
            Write(52, (classGold + originGold).ToString());
            var sheet = c.GetSceneData("cartaPersonaj");
            foreach (int index in new[] { 76, 77, 82 }) Write(index, generated[index]);
            for (int i = 0; i < 4; i++)
            {
                string note = generated[78 + i];
                Write(78 + i, note);
                if (note.Length > 0) SetToggle(sheet, 31 + i, true);
            }
            int weaponMod = CharacterCreationRules.Modifier(s.scores[draft.equipment == 1 ? 1 : 0]);
            Write(67, generated[67]);
            Write(66, CharacterCreationRules.Signed(weaponMod + 2 + (draft.equipment == 1 && draft.fightingStyle == FighterStyle.Archery ? 2 : 0)));
            Write(68, (draft.equipment == 1 ? "1d8" : draft.edition == RulesEdition.Rules2024 ? "2d6" : "1d8") + CharacterCreationRules.Signed(weaponMod + (draft.equipment == 0 && draft.edition == RulesEdition.Rules2014 && draft.fightingStyle == FighterStyle.Dueling ? 2 : 0)));
            Write(75, generated[75]);
            // In this prototype the fighter's Second Wind uses the sheet's generic class resource panel.
            Write(101, generated[101]);
            SetDropdown(sheet, 14, 0); // No named Fighter resource in this selector; start with "Сховати все".
            sheet.SetInt("ResourceDropdownOrder_v2", 1);
            SetDropdown(sheet, 13, s.secondWindUses);
            if (draft.edition == RulesEdition.Rules2024 && draft.species == "dwarf")
            {
                sheet.SetInt("StableField_v1_creation.stonecunning.count", 2);
            }
            if (draft.edition == RulesEdition.Rules2024 && draft.species == "human")
            {
                sheet.SetInt("StableField_v1_creation.heroic.count", 1);
            }
            int arrows = (draft.equipment == 1 ? 20 : 0) +
                (draft.edition == RulesEdition.Rules2024 && draft.background == "soldier" && draft.backgroundEquipmentChoice == 0 ? 20 : 0);
            if (arrows > 0) Write(93, arrows.ToString());
            if (draft.edition == RulesEdition.Rules2024 && draft.equipment == 0) Write(96, "8"); // darts
            sheet.SetInt(bindings.healthPrefix + "maxHealth", s.hitPoints);
            sheet.SetInt(bindings.healthPrefix + "currentHealth", s.hitPoints);
            return c;
        }

        // The sheet's first two note text areas are 198 x 348 at font size 15;
        // the last two are taller. Leave room for wrapping and the paragraph gap.
        internal static string[] PackFeatureNotes(IEnumerable<string> features)
        {
            var notes = new string[4];
            int[] lineBudgets = { 20, 20, 25, 25 };
            int noteIndex = 0;
            foreach (string feature in features.Where(f => !string.IsNullOrWhiteSpace(f)))
            {
                string paragraph = feature.Trim();
                string candidate = string.IsNullOrEmpty(notes[noteIndex]) ? paragraph : notes[noteIndex] + "\n\n" + paragraph;
                if (noteIndex < notes.Length - 1 && EstimateWrappedLines(candidate, 24) > lineBudgets[noteIndex])
                {
                    noteIndex++;
                    candidate = paragraph;
                }
                notes[noteIndex] = candidate;
            }
            for (int i = 0; i < notes.Length; i++) notes[i] ??= "";
            return notes;
        }

        private static int EstimateWrappedLines(string text, int charactersPerLine)
        {
            int lines = 0;
            foreach (string paragraph in text.Split('\n'))
            {
                if (paragraph.Length == 0) { lines++; continue; }
                int used = 0;
                foreach (string word in paragraph.Split(' '))
                {
                    if (used > 0 && used + 1 + word.Length > charactersPerLine)
                    {
                        lines++;
                        used = 0;
                    }
                    used += (used > 0 ? 1 : 0) + word.Length;
                    while (used > charactersPerLine)
                    {
                        lines++;
                        used -= charactersPerLine;
                    }
                }
                lines++;
            }
            return lines;
        }

        private static void SetDropdown(CharacterSceneData data, int index, int value)
        {
            while (data.dropdownData.Count <= index) data.dropdownData.Add(0);
            data.dropdownData[index] = value;
        }
        private static void SetToggle(CharacterSceneData data, int index, bool value)
        {
            while (data.toggleData.Count <= index) data.toggleData.Add(false);
            data.toggleData[index] = value;
        }

        internal bool TryCreate(CharacterCreationDraft draft, out string error, out string id)
        {
            id = null; error = null;
            try
            {
                CharacterData created = Build(draft);
                DndSaveManager manager = new RuntimeManagerResolver().SaveManager();
                manager.FlushPendingSave();
                AppSaveData candidate = TaruckBinaryCodec.DecodeFullSave(TaruckBinaryCodec.EncodeFullSave(manager.GetCollectionForExport(), Application.version));
                candidate.characters.Add(created);
                candidate.lastActiveCharacterId = created.id;
                if (!manager.TryImportCollection(candidate, false, out error)) return false;
                id = created.id;
                return true;
            }
            catch (Exception e) { error = e.Message; return false; }
        }
    }
}
