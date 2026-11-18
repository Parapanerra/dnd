using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterOriginFeatRules
    {
        public static readonly string[] Ids = { "alert", "crafter", "healer", "lucky", "magiccleric", "magicdruid", "magicwizard", "musician", "savage", "skilled", "tavern", "tough" };
        public static readonly string[] Names = { "Пильний", "Майстер-ремісник", "Цілитель", "Щасливець", "Посвячений у магію — жрець", "Посвячений у магію — друїд", "Посвячений у магію — чарівник", "Музикант", "Лютий нападник", "Умілець", "Таверновий забіяка", "Міцний" };
        public static readonly string[] CraftTools = { "Інструменти теслі", "Інструменти шкіряра", "Інструменти каменяра", "Інструменти гончара", "Інструменти коваля", "Інструменти лудильника", "Інструменти ткача", "Інструменти різьбяра" };
        public static string Name(string id) { int i = Array.IndexOf(Ids, id); return i < 0 ? "" : Names[i]; }
        internal static bool Modern(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static bool HumanChoice(CharacterCreationDraft d) => Modern(d) && d.species == "human";
        public static IEnumerable<CharacterOriginFeatChoice> Choices(CharacterCreationDraft d)
        {
            if (!Modern(d)) yield break;
            if (d.backgroundFeat != null && d.backgroundFeat.id.Length > 0) yield return d.backgroundFeat;
            if (HumanChoice(d) && d.humanFeat != null && d.humanFeat.id.Length > 0) yield return d.humanFeat;
            if(CharacterWarlockRules.Modern(d))foreach(var feat in d.warlockLessons??new List<CharacterOriginFeatChoice>())if(feat!=null&&CharacterWarlockRules.Has(d,"lessons:"+feat.id))yield return feat;
        }
        internal static void Repair(CharacterCreationDraft d)
        {
            if (d.backgroundFeat == null) d.backgroundFeat = new CharacterOriginFeatChoice();
            if (d.humanFeat == null) d.humanFeat = new CharacterOriginFeatChoice();
            string expected = Modern(d) ? CharacterBackgroundRules.Find(d)?.feat ?? "" : "";
            if (d.backgroundFeat.id != expected) d.backgroundFeat = new CharacterOriginFeatChoice { id = expected };
            if (HumanChoice(d) && d.humanFeat.id == expected && expected != "skilled") d.humanFeat = new CharacterOriginFeatChoice();
        }
        public static string[] HumanOptions(CharacterCreationDraft d) => Ids.Where(id => id != d.backgroundFeat.id || id == "skilled").ToArray();
        public static bool Has(CharacterCreationDraft d, string id) => Choices(d).Any(choice => choice.id == id);
        public static bool Magic(CharacterOriginFeatChoice choice) => choice.id.StartsWith("magic", StringComparison.Ordinal);
        public static string SpellClass(CharacterOriginFeatChoice choice) => Magic(choice) ? choice.id.Substring(5) : "";
        public static string[] Spells(CharacterOriginFeatChoice choice) => !Magic(choice) ? Array.Empty<string>() : (choice.cantrips ?? Array.Empty<string>()).Concat(new[] { choice.spell }).Where(name => name.Length > 0).ToArray();
        public static int[] Skills(CharacterCreationDraft d) => Choices(d).SelectMany(choice => choice.selections ?? Array.Empty<string>()).Where(key => key.StartsWith("skill:", StringComparison.Ordinal))
            .Select(key => int.TryParse(key.Substring(6), out int index) ? index : -1).Where(index => index >= 0 && index < 18).Distinct().ToArray();
        public static string[] Tools(CharacterCreationDraft d) => Choices(d).SelectMany(choice => choice.selections ?? Array.Empty<string>()).Where(key => key.StartsWith("tool:", StringComparison.Ordinal)).Select(key => key.Substring(5)).Distinct().ToArray();
        public static string[] CraftNotes(CharacterOriginFeatChoice choice) => choice.id != "crafter" ? Array.Empty<string>() : (choice.selections ?? Array.Empty<string>()).Select(key => {
            switch (key)
            {
                case "tool:Інструменти теслі": return "Тесля — швидке майстрування: драбина або смолоскип.";
                case "tool:Інструменти шкіряра": return "Шкіряний майстер — швидке майстрування: футляр болтів, футляр сувоїв або гаманець.";
                case "tool:Інструменти каменяра": return "Каменяр — швидке майстрування: блок і таль.";
                case "tool:Інструменти гончара": return "Гончар — швидке майстрування: глечик або лампа.";
                case "tool:Інструменти коваля": return "Коваль — швидке майстрування: підшипники, відро, шипи, гак-кішка або залізний казан.";
                case "tool:Інструменти лудильника": return "Ремонтник — швидке майстрування: дзвіночок, лопата або кресало.";
                case "tool:Інструменти ткача": return "Ткач — швидке майстрування: кошик, мотузка, сітка або намет.";
                case "tool:Інструменти різьбяра": return "Різьбяр — швидке майстрування: дубина, велика дубина або посох.";
                default: return "";
            }
        }).Where(text => text.Length > 0).ToArray();
        public static string[] Options(CharacterCreationDraft d, CharacterOriginFeatChoice choice)
        {
            var keys = choice.id == "crafter" ? CraftTools.Select(tool => "tool:" + tool) : choice.id == "musician" ? CharacterCreationRules.MonkTools.Skip(17).Select(tool => "tool:" + tool)
                : choice.id == "skilled" ? Enumerable.Range(0, 18).Select(index => "skill:" + index).Concat(CharacterBackgroundRules.AllTools.Where(tool => tool != "Наземний транспорт" && tool != "Водний транспорт").Select(tool => "tool:" + tool)) : Enumerable.Empty<string>();
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(d.humanSkill >= 0 ? new[] { d.humanSkill } : Array.Empty<int>()).Concat(d.knowledgeSkills ?? new List<int>());
            var existing = skills.Select(index => "skill:" + index).Concat(CharacterBackgroundRules.ClassTools(d).Concat(CharacterBackgroundRules.Tools(d)).Select(tool => "tool:" + tool));
            return keys.Except(existing).ToArray();
        }
        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Modern(d)) return null;
            if (page == 1 && HumanChoice(d) && !HumanOptions(d).Contains(d.humanFeat.id)) return "Обери додаткову рису походження людини.";
            if (page != 4) return null;
            var used = new HashSet<string>(StringComparer.Ordinal);
            var native = CharacterSpeciesRules.Spells(d).Concat(d.cantrips).Concat(d.tomeCantrips).ToArray();
            foreach (var choice in Choices(d))
            {
                if (!Ids.Contains(choice.id)) return "Обери рису походження.";
                if (Magic(choice))
                {
                    if (!new[] { 3, 4, 5 }.Contains(choice.ability) || choice.cantrips == null || choice.cantrips.Length != 2 || choice.cantrips.Any(name => name.Length == 0 || native.Contains(name) || !used.Add("spell:" + name)) || string.IsNullOrEmpty(choice.spell)) return "Обери характеристику магії, два заговори та спел риси без повторів.";
                }
                if (choice.id != "skilled" && choice.id != "crafter" && choice.id != "musician") continue;
                string[] options = Options(d, choice);
                if (choice.selections == null || choice.selections.Length != 3 || choice.selections.Any(key => !options.Contains(key) || !used.Add(key))) return "Обери три різні володіння риси без повторів.";
            }
            return null;
        }
        internal static CreatedCharacterStats Apply(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            if (!Modern(d)) return stats;
            if (Has(d, "tough")) stats.hitPoints += 2 * d.TotalLevel;
            if (Has(d, "alert")) stats.initiative += CharacterLegacySpeciesRules.Proficiency(d);
            stats.skills = stats.skills.Concat(Skills(d)).Distinct().ToList();
            stats.passivePerception = 10 + CharacterCreationRules.Modifier(stats.scores[4]) + (stats.expertise != null && stats.expertise.Contains(11) ? 2 * CharacterLegacySpeciesRules.Proficiency(d) : stats.skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0);
            foreach (var choice in Choices(d))
            {
                string text = Feature(choice.id); if (text.Length > 0) stats.features += "\n" + text;
                foreach (string craftNote in CraftNotes(choice)) stats.features += "\n" + craftNote;
            }
            return stats;
        }
        public static string Feature(string id)
        {
            switch (id)
            {
                case "alert": return "Пильний: додай бонус майстерності до ініціативи; після кидка можна обмінятися ініціативою з охочим союзником, якщо обоє не недієздатні.";
                case "tough": return "Міцний: +2 ОЗ за рівень.";
                case "savage": return "Лютий нападник: раз за хід двічі кинь кубики шкоди зброї при влучанні й обери результат.";
                case "lucky": return "Щасливець: очки везіння за бонусом майстерності, довгий відпочинок. Одне очко дає перевагу на твій кидок к20 або заваду атаці проти тебе.";
                case "healer": return "Цілитель: дією Використання витрать заряд набору цілителя; істота у 5 футах витрачає кістку ОЗ й лікується на її кидок + твій бонус майстерності. На кубиках лікування цією рисою або спелом можна перекинути 1, новий результат обов’язковий.";
                case "tavern": return "Таверновий забіяка: беззбройна шкода 1к4 + Сила, можна перекинути 1; володіння імпровізованою зброєю. Раз за свій хід після беззбройного влучання дією Атака можна також відштовхнути ціль на 5 футів.";
                case "musician": return "Музикант: наприкінці короткого або довгого відпочинку зіграй на знайомому інструменті й дай героїчне натхнення союзникам, які чують; кількість за бонусом майстерності.";
                case "crafter": return "Майстер-ремісник: 20% знижки на немагічні речі. Наприкінці довгого відпочинку створи одну річ відповідними знайомими інструментами; розпадається після наступного довгого відпочинку.";
                case "skilled": return "Умілець: отримуєш три обрані володіння навичками, інструментами або їх поєднанням. Цю рису можна брати повторно.";
                case "magiccleric":
                case "magicdruid":
                case "magicwizard":
                    return "Посвячений у магію: два заговори й одне завжди підготовлене заклинання 1 рівня з обраного списку. Його можна накласти раз без комірки за довгий відпочинок або своїми комірками. Використовуй обраний при отриманні риси Інтелект, Мудрість або Харизму. На новому рівні можна замінити одне з цих заклинань іншим того самого рівня з того самого списку.";
                default: return "";
            }
        }
    }
}
