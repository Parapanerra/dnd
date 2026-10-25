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
            var c = new CharacterData(Guid.NewGuid().ToString()) { characterName = draft.characterName.Trim(), currentHealth = s.hitPoints, maxHealth = s.hitPoints };
            c.SetSharedString("CreationWizard_v1", JsonUtility.ToJson(draft));
            c.SetSharedString("RulesEdition", draft.edition == RulesEdition.Rules2024 ? "2024" : "2014");
            void Write(int index, string value) => bindings.Text(c, "input." + index, value);
            Write(57, c.characterName); Write(58, "1"); Write(59, "0"); Write(60, "Воїн");
            Write(63, s.speciesName); Write(64, s.backgroundName); Write(65, draft.alignment);
            Write(62, draft.species == "dwarf" && draft.edition == RulesEdition.Rules2014 ? "Пагорбовий" : "");
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
            Write(52, (draft.edition == RulesEdition.Rules2024 ? 50 + (draft.equipment == 0 ? 4 : 11) : draft.background == "soldier" ? 10 : 15).ToString());
            var sheet = c.GetSceneData("cartaPersonaj");
            string[] featureLines = s.features.Split('\n');
            Write(76, string.Join("\n", featureLines.Take(3)));
            Write(77, s.proficiencies); Write(82, s.equipment);
            string[] notes = PackFeatureNotes(featureLines.Skip(3));
            for (int i = 0; i < 4; i++)
            {
                string note = notes[i];
                Write(78 + i, note);
                if (note.Length > 0) SetToggle(sheet, 31 + i, true);
            }
            int weaponMod = CharacterCreationRules.Modifier(s.scores[draft.equipment == 1 ? 1 : 0]);
            Write(67, draft.equipment == 1 ? "Довгий лук" : draft.edition == RulesEdition.Rules2024 ? "Дворучний меч" : "Довгий меч");
            Write(66, CharacterCreationRules.Signed(weaponMod + 2 + (draft.equipment == 1 && draft.fightingStyle == FighterStyle.Archery ? 2 : 0)));
            Write(68, (draft.equipment == 1 ? "1d8" : draft.edition == RulesEdition.Rules2024 ? "2d6" : "1d8") + CharacterCreationRules.Signed(weaponMod + (draft.equipment == 0 && draft.edition == RulesEdition.Rules2014 && draft.fightingStyle == FighterStyle.Dueling ? 2 : 0)));
            Write(75, draft.fightingStyle == FighterStyle.Archery ? "Стрільба: +2 до атаки далекобійною зброєю (бонус уже враховано для лука)." :
                draft.fightingStyle == FighterStyle.Dueling ? "Дуелі: +2 шкоди при атаці відповідною одноручною зброєю." :
                draft.fightingStyle == FighterStyle.GreatWeapon ? "Велика зброя: особлива дія кубиків шкоди дворучної зброї за правилами вибраної редакції." : "Захист: +1 КЗ у обладунку.");
            // In this prototype the fighter's Second Wind uses the sheet's generic class resource panel.
            Write(101, "Друге дихання");
            SetDropdown(sheet, 14, 11); // "Інша навичка" remains the last option.
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
            if (draft.equipment == 1) Write(93, "20"); // arrows
            if (draft.edition == RulesEdition.Rules2024 && draft.equipment == 0) Write(96, "8"); // darts
            sheet.SetInt(bindings.healthPrefix + "maxHealth", s.hitPoints);
            sheet.SetInt(bindings.healthPrefix + "currentHealth", s.hitPoints);
            return c;
        }

        // The sheet's first two note text areas are 198 x 348 at font size 15;
        // the last two are taller. Leave room for wrapping and the paragraph gap.
        private static string[] PackFeatureNotes(IEnumerable<string> features)
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
