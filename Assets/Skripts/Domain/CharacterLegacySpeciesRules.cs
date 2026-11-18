using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // PHB species not covered by dwarf/elf/gnome families. Choices stay separate from class choices.
    public static class CharacterLegacySpeciesRules
    {
        public static readonly string[] TieflingLineages = { "abyssal", "chthonic", "infernal" };
        public static readonly string[] TieflingNames = { "Безодня", "Хтонічна спадщина", "Інфернальна спадщина" };
        public static readonly string[] GiantLineages = { "cloud", "fire", "frost", "hill", "stone", "storm" };
        public static readonly string[] GiantNames = { "Хмарний велетень", "Вогняний велетень", "Морозний велетень", "Пагорбовий велетень", "Кам’яний велетень", "Штормовий велетень" };
        public static readonly string[] GiantPowers = { "Хмарний стрибок", "Опік вогню", "Морозний холод", "Падіння пагорба", "Кам’яна витривалість", "Грім шторму" };
        internal static bool Modern(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static bool HalfElf(CharacterCreationDraft d) => d.species == "halfelf";
        public static bool NeedsSize(CharacterCreationDraft d) => CharacterExpandedSpeciesRules.NeedsSize(d)||Modern(d) && (d.species == "human" || d.species == "aasimar" || d.species == "tiefling" || d.species == "warforged");
        public static int Proficiency(CharacterCreationDraft d) => Proficiency(d.TotalLevel);
        public static int Proficiency(int characterLevel) => 2 + (Math.Max(1, characterLevel) - 1) / 4;
        internal static string LineageName(CharacterCreationDraft d)
        {
            if (d.species == "dragonborn") return d.speciesAncestor >= 0 && d.speciesAncestor < 10 ? CharacterSorcererRules.DragonNames[d.speciesAncestor] : "";
            var keys = d.species == "goliath" ? GiantLineages : d.species == "tiefling" && Modern(d) ? TieflingLineages : Array.Empty<string>();
            int index = Array.IndexOf(keys, d.speciesLineage);
            return index < 0 ? "" : d.species == "goliath" ? GiantNames[index] : TieflingNames[index];
        }
        internal static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (page == 1)
            {
                if (d.species == "dragonborn" && (d.speciesAncestor < 0 || d.speciesAncestor > 9)) return "Обери драконячого предка раси.";
                if (d.species == "goliath" && !GiantLineages.Contains(d.speciesLineage)) return "Обери велетенську спадщину.";
                if (d.species == "tiefling" && Modern(d) && (!TieflingLineages.Contains(d.speciesLineage) || !new[] { 3, 4, 5 }.Contains(d.speciesMagicAbility))) return "Обери спадщину тифлінга та характеристику її магії.";
                if (NeedsSize(d) && d.speciesSize != "small" && d.speciesSize != "medium") return "Обери розмір персонажа.";
                if (HalfElf(d) && (d.speciesIncreases == null || d.speciesIncreases.Length != 2 || d.speciesIncreases.Any(i => i < 0 || i > 4) || d.speciesIncreases.Distinct().Count() != 2)) return "Обери дві різні характеристики напівельфа для +1.";
            }
            if (page == 3 && HalfElf(d))
            {
                if (d.speciesSkills == null || d.speciesSkills.Length != 2 || d.speciesSkills.Any(i => i < 0 || i > 17 || CharacterCreationRules.OriginalBackgroundSkills(d).Contains(i) || d.classSkills.Contains(i)) || d.speciesSkills.Distinct().Count() != 2) return "Обери дві різні навички напівельфа без повторів.";
                if (string.IsNullOrEmpty(d.speciesLanguage) || d.speciesLanguage == "Загальна" || d.speciesLanguage == "Ельфійська") return "Обери додаткову мову напівельфа.";
            }
            return null;
        }
        internal static void Bonuses(CharacterCreationDraft d, int[] scores)
        {
            if (Modern(d)) return;
            if (d.species == "dragonborn") { scores[0] += 2; scores[5]++; }
            if (d.species == "tiefling") { scores[5] += 2; scores[3]++; }
            if (HalfElf(d))
            {
                scores[5] += 2;
                foreach (int i in (d.speciesIncreases ?? Array.Empty<int>()).Distinct().Where(i => i >= 0 && i < 5)) scores[i]++;
            }
        }
        internal static string FreeSpell(CharacterCreationDraft d) => d.species != "tiefling" || d.TotalLevel < 3 ? "" : !Modern(d) ? "Hellish Rebuke" :
            d.speciesLineage == "abyssal" ? "Ray of Sickness" : d.speciesLineage == "chthonic" ? "False Life" : "Hellish Rebuke";
        internal static string[] Spells(CharacterCreationDraft d)
        {
            if (d.species == "aasimar") return new[] { "Light" };
            if (d.species != "tiefling") return Array.Empty<string>();
            return new[] { "Thaumaturgy", Modern(d) ? d.speciesLineage == "abyssal" ? "Poison Spray" : d.speciesLineage == "chthonic" ? "Chill Touch" : "Fire Bolt" : "", FreeSpell(d) }.Where(s => s.Length > 0).ToArray();
        }
        internal static int MagicAbility(CharacterCreationDraft d) => d.species == "tiefling" && Modern(d) ? d.speciesMagicAbility : 5;
        public static string BreathDice(CharacterCreationDraft d) => Modern(d) ? (d.TotalLevel >= 17 ? 4 : d.TotalLevel >= 11 ? 3 : d.TotalLevel >= 5 ? 2 : 1) + "d10" : (d.TotalLevel >= 16 ? 5 : d.TotalLevel >= 11 ? 4 : d.TotalLevel >= 6 ? 3 : 2) + "d6";
        public static string BreathType(CharacterCreationDraft d) => d.speciesAncestor == 0 || d.speciesAncestor == 4 ? "Кислотна шкода" : d.speciesAncestor == 1 || d.speciesAncestor == 3 ? "Блискавична шкода" : d.speciesAncestor == 6 ? "Отруйна шкода" : d.speciesAncestor >= 8 ? "Холодова шкода" : "Вогняна шкода";
        public static string GiantPower(CharacterCreationDraft d) { int i = Array.IndexOf(GiantLineages, d.speciesLineage); return i < 0 ? "" : GiantPowers[i]; }
        public static string Features(CharacterCreationDraft d)
        {
            if (HalfElf(d)) return "\nНапівельф: Середній розмір, темнозір 60 футів. Перевага проти зачарування; магія не може приспати.";
            if (d.species == "dragonborn") return "\nДракононароджений: Середній розмір." + (Modern(d) ? "\nТемнозір 60 футів.\nДихання дракона: заміни одну атаку конусом 15 футів або лінією 30×5 футів; ряткидок Спритності, успіх — половина шкоди. Використання за бонусом майстерності, довгий відпочинок."
                : "\nДихання дракона: дія; ряткидок, успіх — половина шкоди. Одне використання за короткий або довгий відпочинок.");
            if (d.species == "tiefling") return "\nТифлінг: темнозір 60 футів." + (Modern(d) ? d.speciesLineage == "abyssal" ? "\nОпір отруйній шкоді." : d.speciesLineage == "chthonic" ? "\nОпір некротичній шкоді." : "\nОпір вогняній шкоді." : "\nОпір вогняній шкоді.")+"\n"+CharacterDescriptionText.Get(Modern(d)?"lineage.magic24":"tiefling.magic14");
            if (d.species == "aasimar") return "\nАсімар: темнозір 60 футів; опір некротичній та променистій шкоді.\n"+CharacterDescriptionText.Get("aasimar.healing")+(d.TotalLevel>=3?"\n"+CharacterDescriptionText.Get("aasimar.revelation")+"\n"+CharacterDescriptionText.Get("aasimar.options"):"");
            if (d.species != "goliath") return "";
            string power = d.speciesLineage == "cloud" ? "Хмарний стрибок: бонусною дією телепортуйся на 30 футів у видиме вільне місце."
                : d.speciesLineage == "fire" ? "Опік вогню: після влучання атакою додай 1к10 вогняної шкоди."
                : d.speciesLineage == "frost" ? "Морозний холод: після влучання додай 1к6 холодової шкоди та зменш швидкість цілі на 10 футів до початку твого наступного ходу."
                : d.speciesLineage == "hill" ? "Падіння пагорба: після влучання атакою і шкоди збий з ніг Велику або меншу ціль."
                : d.speciesLineage == "stone" ? "Кам’яна витривалість: реакцією зменш отриману шкоду на 1к12 + мод Статури."
                : "Грім шторму: після отримання шкоди від істоти у 60 футах реакцією завдай їй 1к8 громової шкоди.";
            return (d.TotalLevel >= 5 ? "\nВелика форма: бонусною дією стань Великим на 10 хвилин, якщо достатньо місця; перевага перевірок Сили й +10 футів швидкості. Раз за довгий відпочинок." : "") + "\nГоліаф: Середній розмір, швидкість 35 футів. Потужна статура: перевага на звільнення від захоплення; вантажність як для більшого розміру.\n" + power + "\nВелетенська спадщина: використання за бонусом майстерності, відновлення після довгого відпочинку.";
        }
    }
}
