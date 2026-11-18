using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class ReadyFormProgressionNotes
    {
        private const string OldSpirit = "Базова комірка: {0}. За кожен вищий круг ОЗ +{1}, КО +1, шкода атаки +1; атак за дію = половина круга вниз. Після накладання вищою коміркою зміни ці значення у формі.";
        private static readonly int[] Fields = { 41, 42, 43 };
        internal static string SlotGrowth(int circle, int hp, int ac, int hpStep, string damage, Func<string, string> translate)
        {
            return string.Format(translate("Комірка {0}: {1} ОЗ · КО {2} · шкода {3}."), circle, hp, ac, damage)
                + "\n\n" + string.Format(translate("За кожен рівень комірки вище базового: +{0} ОЗ, +1 КО, +1 до шкоди основної атаки."), hpStep);
        }
        private static string Caster(bool charisma, Func<string, string> t, bool wisdom = false) =>
            t(wisdom ? "Атака = БМ + мод. Мудрості.\nСК = 8 + БМ + мод. Мудрості." : charisma ? "Атака = БМ + мод. Харизми.\nСК = 8 + БМ + мод. Харизми." : "Атака = БМ + мод. Інтелекту.\nСК = 8 + БМ + мод. Інтелекту.") + "\n\n" +
            t("БМ за рівнем персонажа:\n1–4: +2; 5–8: +3; 9–12: +4;\n13–16: +5; 17–20: +6.") + "\n\n" +
            t("Рівень персонажа сам по собі не збільшує ОЗ і КО цієї форми: їх визначає комірка.");
        private static void Write(CharacterData character, CreationSheetBindings bindings, int slot, params string[] notes)
        {
            for (int i = 0; i < notes.Length && i < Fields.Length; i++)
                bindings.Text(character, "form.input." + Fields[i], notes[i], WildShapeFormList.SceneNameForSlot(slot));
        }
        internal static void Spirit(CharacterData character, CreationSheetBindings bindings, int slot, int first, int hp, int ac, int hpStep, string damage, Func<string, string> t, bool wisdom = false, bool charisma = false)
        {
            Write(character, bindings, slot, SlotGrowth(first, hp, ac, hpStep, damage, t), Caster(charisma, t, wisdom),
                t("Атак за дію = половина круга комірки, округлення вниз.") + "\n\n" + t("Після зміни комірки або показників мага онови поля форми вручну."));
        }
        internal static void Paladin(CharacterData character, CreationSheetBindings bindings, int slot, bool revised, string id, int hp, int ac, string damage, Func<string, string> t)
        {
            if (!revised)
            {
                Write(character, bindings, slot,
                    t("Поклик скакуна 2014: вища комірка та рівень паладина не збільшують показники скакуна. Використовуй звичайний блок обраної істоти."),
                    t("Атаки й ряткидки цього скакуна мають власні бонуси з його блоку. Не додавай до них Харизму або зростання бонусу майстерності паладина."));
                return;
            }
            bool spirit = id.StartsWith("spirit.", StringComparison.Ordinal);
            string detail = t(spirit ? "Атак за дію = половина круга комірки, округлення вниз." : "Потойбічний удар: одна атака за дію. Комірка 4 або вище додає політ 60 футів.");
            if (spirit || id == "steed.celestial") detail += "\n\n" + t("Цілющий дотик відновлює 2d8 + круг комірки ОЗ. Наприклад, комірка 5: 2d8+5; комірка 9: 2d8+9.");
            detail += "\n\n" + t("Після зміни комірки або показників мага онови поля форми вручну.");
            Write(character, bindings, slot, SlotGrowth(spirit ? 5 : 2, hp, ac, 10, damage, t), Caster(true, t), detail);
        }
        private static bool Replaceable(string text, Func<string, string> t)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            // Replace only the exact old generated cannon table, preserving edited notes.
            var cannon = new List<string> { t(CharacterArtificerText.Get("form.table.level")) };
            for (int level = 3; level <= 20; level++)
                cannon.Add(level + ": " + (5 * level) + " · 18 · " + (level < 9 ? "2d8" : "3d8"));
            if (text == string.Join("\n", cannon)) return true;
            if (new[] { "form.reference", "form.upcast.steed", "form.upcast.spirit" }.Any(id => text == t(CharacterPaladinText.Get(id)))) return true;
            return new[] { 3, 4 }.Any(circle => new[] { 10, 15 }.Any(hp => text == string.Format(t(OldSpirit), circle, hp)));
        }
        internal static bool UpgradeExisting(CharacterData character, CreationSheetBindings bindings,
            ReadyFormTemplate[] templates, AppLanguage language)
        {
            bool changed = false;
            var catalog = new TranslationCatalog();
            Func<string, string> translate = text => catalog.Translate(text, language);
            foreach (var state in character.sceneStates.ToArray())
            {
                int slot = WildShapeFormList.SlotFromSceneName(state.sceneName);
                if (slot < 0) continue;
                string id = character.GetSharedString(ReadyFormCatalog.Key(slot), "");
                var template = templates.FirstOrDefault(item => item.id == id);
                if (template == null) continue;
                var probe = new CharacterData("progression-notes");
                probe.SetSharedString("CreationWizard_v1", character.GetSharedString("CreationWizard_v1", ""));
                template.Write(probe, bindings, slot, translate);
                var expected = probe.GetSceneData(state.sceneName, false);
                foreach (int index in Fields)
                {
                    var field = bindings.Entries.First(entry => entry.key == "form.input." + index);
                    string key = "StableField_v1_" + field.stableId;
                    string current = state.GetString(key, field.index >= 0 && field.index < state.inputData.Count ? state.inputData[field.index] : "");
                    string next = expected.GetString(key, "");
                    if (current == next || string.IsNullOrEmpty(next) || !IsGeneratedLegacyText(current, catalog)) continue;
                    bindings.Text(character, "form.input." + index, next, state.sceneName);
                    changed = true;
                }
            }
            return changed;
        }
        internal static bool IsNoteField(CreationSheetBindings bindings, string stableId)
        {
            return bindings.Entries.Any(entry => entry.stableId == stableId &&
                Fields.Any(index => entry.key == "form.input." + index));
        }

        internal static bool IsGeneratedLegacyText(string current, TranslationCatalog catalog) =>
            Enum.GetValues(typeof(AppLanguage)).Cast<AppLanguage>()
                .Any(language => Replaceable(current, text => catalog.Translate(text, language)));
    }
}
