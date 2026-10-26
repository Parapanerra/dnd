using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Translates generated descriptions while leaving draft choices and rule data canonical.
    internal static class CharacterCreationPresentation
    {
        internal static string FeatureLines(string source, Func<string, string> translate) =>
            string.Join("\n", source.Split('\n').Select(translate));

        internal static string Proficiencies(CharacterCreationDraft draft, CreatedCharacterStats stats, Func<string, string> translate)
        {
            bool modern = draft.edition == RulesEdition.Rules2024;
            bool dwarf = draft.species == "dwarf";
            string tools = draft.background == "soldier"
                ? translate(draft.gamingSet) + (modern ? "." : translate(", наземний транспорт."))
                : translate("Злодійські інструменти") + (modern ? "." : ", " + translate(draft.gamingSet) + ".");
            if (dwarf && !modern)
                tools += " " + translate(draft.artisanTool) + translate("; бойова сокира, ручна сокира, легкий і бойовий молоти.");
            string languages = translate("Загальна") + ", " + (modern
                ? translate(draft.language) + ", " + translate(draft.secondLanguage)
                : dwarf ? translate("Дварфська") : translate(draft.language));
            return translate("Усі обладунки, щити, проста й військова зброя. Ряткидки: Сила, Статура.") +
                "\n" + translate("Навички:") + " " + string.Join(", ", stats.skills.Select(index => translate(CharacterCreationRules.SkillNames[index]))) +
                ".\n" + tools + "\n" + translate("Мови:") + " " + languages + ".";
        }

        internal static string Equipment(CharacterCreationDraft draft, Func<string, string> translate)
        {
            bool modern = draft.edition == RulesEdition.Rules2024;
            string kit = modern
                ? translate(draft.equipment == 0 ? "Кольчуга; дворучний меч; ціп; 8 дротиків; набір підземельника; 4 зм."
                    : "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; набір підземельника; 11 зм.")
                : translate(draft.equipment == 0 ? "Кольчуга; довгий меч; щит; 2 ручні сокири; набір підземельника."
                    : "Шкіряний обладунок; довгий лук; 20 стріл; довгий меч; щит; 2 ручні сокири; набір підземельника.");
            string origin = modern
                ? draft.backgroundEquipmentChoice < 0 ? translate("Обери спорядження або 50 зм.")
                    : draft.backgroundEquipmentChoice == 1 ? translate("50 зм замість спорядження передісторії.")
                    : draft.background == "soldier"
                        ? Format(translate, "Спис; короткий лук; 20 стріл; {0}; набір цілителя; сагайдак; дорожній одяг; 14 зм.", translate(draft.gamingSet))
                        : translate("2 кинджали; злодійські інструменти; ломик; 2 гаманці; дорожній одяг; 16 зм.")
                : draft.background == "soldier"
                    ? Format(translate, "Знак звання; трофей; {0}; звичайний одяг; гаманець із 10 зм.", translate(draft.gamingSet))
                    : translate("Ломик; темний одяг із каптуром; гаманець із 15 зм.");
            return kit + "\n" + translate("Передісторія:") + " " + origin;
        }

        internal static Dictionary<int, string> GeneratedSheetFields(CharacterCreationDraft draft, CreatedCharacterStats stats, Func<string, string> translate)
        {
            string[] features = FeatureLines(stats.features, translate).Split('\n');
            string[] notes = CharacterCreationService.PackFeatureNotes(features.Skip(3));
            var fields = new Dictionary<int, string> {
                [60] = translate("Воїн"),
                [62] = draft.species == "dwarf" && draft.edition == RulesEdition.Rules2014 ? translate("Пагорбовий") : "",
                [63] = translate(stats.speciesName),
                [64] = translate(stats.backgroundName),
                [65] = translate(draft.alignment),
                [67] = translate(draft.equipment == 1 ? "Довгий лук" : draft.edition == RulesEdition.Rules2024 ? "Дворучний меч" : "Довгий меч"),
                [75] = translate(draft.fightingStyle == FighterStyle.Archery ? "Стрільба: +2 до атаки далекобійною зброєю (бонус уже враховано для лука)." :
                    draft.fightingStyle == FighterStyle.Dueling ? "Дуелі: +2 шкоди при атаці відповідною одноручною зброєю." :
                    draft.fightingStyle == FighterStyle.GreatWeapon ? "Велика зброя: особлива дія кубиків шкоди дворучної зброї за правилами вибраної редакції." : "Захист: +1 КО у обладунку."),
                [76] = string.Join("\n", features.Take(3)),
                [77] = Proficiencies(draft, stats, translate),
                [82] = Equipment(draft, translate),
                [101] = translate("Друге дихання")
            };
            for (int i = 0; i < notes.Length; i++) fields[78 + i] = notes[i];
            return fields;
        }

        private static string Format(Func<string, string> translate, string source, params object[] values) =>
            string.Format(translate(source), values);
    }
}
