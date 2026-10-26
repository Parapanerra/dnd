using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // First vertical slice: Fighter 1, Human/Hill Dwarf, Soldier/Criminal.
    // All calculations are independent of Unity and of the sheet's layout.
    public static class CharacterCreationRules
    {
        public static readonly string[] AbilityNames = { "Сила", "Спритність", "Статура", "Інтелект", "Мудрість", "Харизма" };
        public static readonly string[] SkillNames = { "Акробатика", "Догляд тварин", "Магія", "Атлетика", "Обман", "Історія", "Аналіз поведінки", "Залякування", "Аналіз", "Медицина", "Природа", "Уважність", "Артистичність", "Переконливість", "Релігія", "Спритність рук", "Скритність", "Виживання" };
        public static readonly int[] SkillAbilities = { 1, 4, 3, 0, 5, 3, 4, 5, 3, 4, 3, 4, 5, 5, 3, 1, 1, 4 };
        public static readonly int[] FighterSkills = { 1, 3, 5, 6, 7, 11, 17, 0 };
        private static readonly int[] Costs = { 0, 1, 2, 3, 4, 5, 7, 9 };
        public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2d);
        public static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
        public static int PointCost(int score) => score >= 8 && score <= 15 ? Costs[score - 8] : 1000;
        public static int[] BackgroundSkills(CharacterCreationDraft d) => d.background == "soldier"
            ? new[] { 3, 7 } : d.background == "criminal" ? d.edition == RulesEdition.Rules2024 ? new[] { 15, 16 } : new[] { 4, 16 } : Array.Empty<int>();
        public static int[] AllowedIncreases(CharacterCreationDraft d) => d.background == "soldier" ? new[] { 0, 1, 2 } : d.background == "criminal" ? new[] { 1, 2, 3 } : Array.Empty<int>();

        public static int[] Scores(CharacterCreationDraft d)
        {
            int[] result = (int[])d.abilities.Clone();
            if (d.edition == RulesEdition.Rules2014 && (d.species == "human" || d.species == "dwarf"))
            {
                if (d.species == "human") for (int i = 0; i < 6; i++) result[i]++;
                else { result[2] += 2; result[4]++; }
            }
            else if (d.edition == RulesEdition.Rules2024 && d.bonusMode >= 0)
            {
                if (d.threeIncreases)
                    foreach (int index in AllowedIncreases(d)) result[index]++;
                else
                {
                    if (d.increaseTwo >= 0 && d.increaseTwo < 6) result[d.increaseTwo] += 2;
                    if (d.increaseOne >= 0 && d.increaseOne < 6) result[d.increaseOne]++;
                }
            }
            return result;
        }

        public static void RepairDependentChoices(CharacterCreationDraft d)
        {
            int[] allowed = AllowedIncreases(d);
            if (!allowed.Contains(d.increaseTwo)) d.increaseTwo = -1;
            if (!allowed.Contains(d.increaseOne) || d.increaseOne == d.increaseTwo) d.increaseOne = -1;
            d.thirdIncrease = -1;
            var background = BackgroundSkills(d);
            d.classSkills.RemoveAll(x => background.Contains(x) || !FighterSkills.Contains(x));
            if (d.species == "human" && d.edition == RulesEdition.Rules2024 &&
                (background.Contains(d.humanSkill) || d.classSkills.Contains(d.humanSkill)))
                d.humanSkill = -1;
        }

        public static List<string> Validate(CharacterCreationDraft d, bool requireName = true)
        {
            var errors = new List<string>();
            if (d == null) { errors.Add("Немає чернетки персонажа."); return errors; }
            if (!Enum.IsDefined(typeof(RulesEdition), d.edition) || !Enum.IsDefined(typeof(AbilityMethod), d.abilityMethod) || !Enum.IsDefined(typeof(FighterStyle), d.fightingStyle)) errors.Add("Невідомі правила або спосіб розрахунку.");
            if (requireName && (string.IsNullOrWhiteSpace(d.characterName) || d.characterName.Trim().Length > 80)) errors.Add("Введи ім’я персонажа: від 1 до 80 символів.");
            if (d.species != "human" && d.species != "dwarf") errors.Add("Обери людину або дварфа.");
            if (d.background != "soldier" && d.background != "criminal") errors.Add("Обери передісторію.");
            if (d.abilities == null || d.abilities.Length != 6) { errors.Add("Потрібно шість характеристик."); return errors; }
            if (d.abilities.Any(x => x == 0)) errors.Add("Розподіли всі шість значень характеристик.");
            else if (d.abilities.Any(x => x < 3 || x > 18)) errors.Add("Базові характеристики: від 3 до 18.");
            if (d.abilityMethod == AbilityMethod.StandardArray && !d.abilities.OrderBy(x => x).SequenceEqual(new[] { 8, 10, 12, 13, 14, 15 })) errors.Add("Стандартний набір: 15, 14, 13, 12, 10, 8 — кожне число один раз.");
            if (d.abilityMethod == AbilityMethod.PointBuy && (d.abilities.Any(x => x < 8 || x > 15) || d.abilities.Sum(PointCost) > 27)) errors.Add("Купівля: значення 8–15 та максимум 27 очок.");
            if (d.abilityMethod == AbilityMethod.Manual &&
                (d.rolledScores == null || d.rolledScores.Length != 6 || d.rolledScores.Any(x => x < 3 || x > 18) ||
                 d.rolledAssignment == null || d.rolledAssignment.Length != 6 ||
                 d.rolledAssignment.Any(x => x < 0 || x > 5) || d.rolledAssignment.Distinct().Count() != 6 ||
                 Enumerable.Range(0, 6).Any(i => d.abilities[i] != d.rolledScores[d.rolledAssignment[i]])))
                errors.Add("Кубики: згенеруй шість значень і розподіли кожне рівно один раз.");
            if (d.edition == RulesEdition.Rules2024)
            {
                int[] allowed = AllowedIncreases(d);
                if (d.bonusMode < 0 || allowed.Length != 3 || !d.threeIncreases &&
                    (!allowed.Contains(d.increaseTwo) || !allowed.Contains(d.increaseOne) || d.increaseTwo == d.increaseOne))
                    errors.Add("Обери бонуси характеристик передісторії.");
            }
            if (Scores(d).Any(x => x > 20)) errors.Add("Після бонусів характеристика не може перевищувати 20.");
            int[] bg = BackgroundSkills(d);
            if (d.classSkills == null || d.classSkills.Count != 2 || d.classSkills.Distinct().Count() != 2 || d.classSkills.Any(x => !FighterSkills.Contains(x) || bg.Contains(x))) errors.Add("Обери дві різні навички воїна без повторення передісторії.");
            if (d.species == "human" && d.edition == RulesEdition.Rules2024 && (d.humanSkill < 0 || d.humanSkill > 17 || bg.Contains(d.humanSkill) || (d.classSkills != null && d.classSkills.Contains(d.humanSkill)))) errors.Add("Додаткова навичка людини має бути унікальною.");
            if (d.equipment < 0 || d.equipment > 1) errors.Add("Обери набір спорядження.");
            if (d.edition == RulesEdition.Rules2024 && d.backgroundEquipmentChoice < 0) errors.Add("Обери спорядження або 50 зм передісторії.");
            if (d.edition == RulesEdition.Rules2024 && (int)d.fightingStyle < 0) errors.Add("Обери бойовий стиль.");
            if (string.IsNullOrWhiteSpace(d.language)) errors.Add("Обери додаткову мову.");
            if (d.edition == RulesEdition.Rules2024 && (string.IsNullOrWhiteSpace(d.secondLanguage) || d.language == d.secondLanguage)) errors.Add("Обери дві різні мови крім Загальної.");
            if (d.edition == RulesEdition.Rules2014 && d.species == "human" && d.language == "Загальна") errors.Add("Обери додаткову мову крім Загальної.");
            if (string.IsNullOrWhiteSpace(d.alignment)) errors.Add("Обери світогляд.");
            return errors;
        }

        public static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] s = Scores(d);
            var skills = BackgroundSkills(d).Concat(d.classSkills).ToList();
            bool modern = d.edition == RulesEdition.Rules2024;
            bool dwarf = d.species == "dwarf";
            if (modern && !dwarf) skills.Add(d.humanSkill);
            int armor = d.equipment == 0 ? (modern ? 16 : 18) : (modern ? 12 : 11) + Modifier(s[1]);
            var result = new CreatedCharacterStats {
                scores = s, skills = skills,
                hitPoints = 10 + Modifier(s[2]) + (dwarf ? 1 : modern ? 2 : 0),
                armorClass = armor + (d.fightingStyle == FighterStyle.Defense ? 1 : 0),
                initiative = Modifier(s[1]) + (modern && d.background == "criminal" ? 2 : 0),
                speed = dwarf && !modern ? 25 : 30,
                passivePerception = 10 + Modifier(s[4]) + (skills.Contains(11) ? 2 : 0),
                secondWindUses = modern ? 2 : 1,
                speciesName = dwarf ? (modern ? "Дварф" : "Дварф (пагорбовий)") : "Людина",
                backgroundName = d.background == "soldier" ? "Солдат" : "Злочинець"
            };
            if (d.equipment == 0 && s[0] < 13 && !(dwarf && !modern)) result.speed -= 10;
            result.equipment = Equipment(d);
            string style = new[] { "Захист: +1 КО у обладунку", "Дуелі: +2 шкоди одноручною зброєю за умов стилю", "Стрільба: +2 до атак далекобійною зброєю", modern ? "Велика зброя: значення 1–2 на кубиках шкоди стають 3 за умов стилю" : "Велика зброя: перекидання 1–2 на кубиках шкоди за умов стилю" }[(int)d.fightingStyle];
            result.features = "Воїн 1 • D&D " + (modern ? "2024" : "2014") + "\n" + style +
                "\nДруге дихання: бонусна дія, відновлює 1к10+1 ОЗ. " + (modern ? "2 використання; короткий відпочинок повертає 1, довгий — усі." : "1 використання; відновлення після короткого або довгого відпочинку.");
            if (modern) result.features += "\nМайстерність зброї: " + (d.equipment == 0 ? "дворучний меч (ковзний удар), ціп (послаблення), дротик (уповільнення)." : "кривий меч (додаткова атака), короткий меч (уразливість), довгий лук (уповільнення).") +
                (d.background == "soldier" ? "\nЛютий нападник: раз за хід двічі кинь кубики шкоди зброї при влучанні й обери результат." : "\nПильний: +2 до ініціативи; обмін ініціативою з охочим союзником за умов риси.");
            else result.features += d.background == "soldier" ? "\nВійськове звання: особливість передісторії солдата." : "\nКримінальний зв’язок: особливість передісторії злочинця.";
            if (dwarf) result.features += modern ? "\nТемнозір 120 футів; опір отруті та перевага проти стану Отруєний.\nМіцність дварфа: +1 ОЗ за рівень. Чуття каменю: треморчуття 60 футів, 10 хв, 2/довгий відпочинок на камені." : "\nТемнозір 60 футів; опір отруті та перевага проти отрути.\nМіцність дварфа: +1 ОЗ за рівень. Чуття каменю: подвійна майстерність до Історії кам’яної кладки.";
            else if (modern) result.features += "\nЛюдина: героїчне натхнення після довгого відпочинку; додаткова навичка.\nПоходження людини — Міцний (рекомендовано): +2 ОЗ за рівень.";
            result.proficiencies = "Усі обладунки, щити, проста й військова зброя. Ряткидки: Сила, Статура.\nНавички: " + string.Join(", ", skills.Select(x => SkillNames[x])) + ".\n" +
                (d.background == "soldier" ? d.gamingSet + (modern ? "." : ", наземний транспорт.") : "Злодійські інструменти" + (modern ? "." : ", " + d.gamingSet + ".")) +
                (dwarf && !modern ? " " + d.artisanTool + "; бойова сокира, ручна сокира, легкий і бойовий молоти." : "") +
                "\nМови: Загальна, " + (modern ? d.language + ", " + d.secondLanguage : dwarf ? "Дварфська" : d.language) + ".";
            return result;
        }

        public static string Equipment(CharacterCreationDraft d)
        {
            bool modern = d.edition == RulesEdition.Rules2024;
            string kit = modern ? (d.equipment == 0 ? "Кольчуга; дворучний меч; ціп; 8 дротиків; набір підземельника; 4 зм." : "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; набір підземельника; 11 зм.") :
                (d.equipment == 0 ? "Кольчуга; довгий меч; щит; 2 ручні сокири; набір підземельника." : "Шкіряний обладунок; довгий лук; 20 стріл; довгий меч; щит; 2 ручні сокири; набір підземельника.");
            string origin = modern
                ? d.backgroundEquipmentChoice < 0 ? "Обери спорядження або 50 зм."
                    : d.backgroundEquipmentChoice == 1 ? "50 зм замість спорядження передісторії."
                    : d.background == "soldier"
                        ? "Спис; короткий лук; 20 стріл; " + d.gamingSet + "; набір цілителя; сагайдак; дорожній одяг; 14 зм."
                        : "2 кинджали; злодійські інструменти; ломик; 2 гаманці; дорожній одяг; 16 зм."
                : d.background == "soldier" ? "Знак звання; трофей; " + d.gamingSet + "; звичайний одяг; гаманець із 10 зм."
                    : "Ломик; темний одяг із каптуром; гаманець із 15 зм.";
            return kit + "\nПередісторія: " + origin;
        }
    }
}
