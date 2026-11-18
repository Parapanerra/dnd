using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterWarforgedRules
    {
        public static bool Warforged(CharacterCreationDraft d) => d.species == "warforged";
        internal static bool Modern(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static string[] ToolOptions(CharacterCreationDraft d) => CharacterBackgroundRules.AllTools
            .Where(tool => tool != "Наземний транспорт" && tool != "Водний транспорт")
            .Except(CharacterBackgroundRules.ClassTools(d, false)).Except(CharacterBackgroundRules.Tools(d)).Except(CharacterOriginFeatRules.Tools(d)).ToArray();
        internal static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Warforged(d)) return null;
            if (page == 1 && !Modern(d) && (d.speciesIncreases == null || d.speciesIncreases.Length < 1 || !new[] { 0, 1, 3, 4, 5 }.Contains(d.speciesIncreases[0]))) return "Обери характеристику кованого для +1, крім Статури.";
            if (page == 3 && (d.speciesSkill < 0 || d.speciesSkill > 17 || CharacterCreationRules.OriginalBackgroundSkills(d).Contains(d.speciesSkill) || d.classSkills.Contains(d.speciesSkill))) return "Обери навичку кованого без повторів.";
            if (page == 3 && !ToolOptions(d).Contains(d.speciesTool)) return "Обери інструмент кованого без повторів.";
            return null;
        }
        public static string Features(CharacterCreationDraft d) => !Warforged(d) ? "" : "\n" + (Modern(d)
            ? "Кований: тип істоти — конструкт. Опір отруйній шкоді, перевага проти стану Отруєний."
            : "Кований: тип істоти — гуманоїд, Середній розмір. Опір отруйній шкоді, перевага проти отруєння; імунітет до хвороб і магічного старіння. Їжа, вода й дихання не потрібні.") + "\nВбудований захист: +1 КО; поки ти живий, обладунок не можна зняти проти твоєї волі." + (Modern(d)
            ? "\nВідпочинок вартового: не потребуєш сну, магія не присипляє; довгий відпочинок — 6 год нерухомості, залишаєшся при свідомості.\nНевтомність: не отримуєш виснаження від зневоднення, недоїдання чи задухи."
            : "\nВбудований обладунок: тільки той, яким володієш; вбудовування та зняття по 1 год, можна під час відпочинку.\nВідпочинок вартового: замість сну щонайменше 6 год нерухомості; залишаєшся при свідомості й бачиш та чуєш. Магія не присипляє.");
    }
}
