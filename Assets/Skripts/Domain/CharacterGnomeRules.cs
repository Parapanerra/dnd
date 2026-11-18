using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterGnomeRules
    {
        public static readonly string[] Lineages = { "forest", "rock" };
        public static readonly string[] LineageNames = { "Лісовий гном", "Скельний гном" };
        public static bool Gnome(CharacterCreationDraft d) => d.species == "gnome";
        internal static bool Modern(CharacterCreationDraft d) => Gnome(d) && d.edition == RulesEdition.Rules2024;
        public static bool ClassicRock(CharacterCreationDraft d) => Gnome(d) && !Modern(d) && d.speciesLineage == "rock";
        public static bool NeedsToolReplacement(CharacterCreationDraft d) => ClassicRock(d) && d.characterClass == "artificer";
        public static string[] ToolReplacements(CharacterCreationDraft d) => CharacterCreationRules.MonkTools.Where(tool => tool != "Інструменти лудильника" && tool != d.artificerTool).ToArray();
        internal static int MagicAbility(CharacterCreationDraft d) => Modern(d) ? d.speciesMagicAbility : 3;
        internal static string FreeSpell(CharacterCreationDraft d) => Modern(d) && d.speciesLineage == "forest" ? "Speak with Animals" : "";
        internal static string[] Spells(CharacterCreationDraft d) => !Gnome(d) ? Array.Empty<string>() : d.speciesLineage == "forest"
            ? Modern(d) ? new[] { "Minor Illusion", "Speak with Animals" } : new[] { "Minor Illusion" }
            : d.speciesLineage == "rock" && Modern(d) ? new[] { "Mending", "Prestidigitation" } : Array.Empty<string>();
        internal static string LineageName(CharacterCreationDraft d)
        {
            int index = Array.IndexOf(Lineages, d.speciesLineage);
            return index >= 0 ? LineageNames[index] : "";
        }
        internal static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Gnome(d)) return null;
            if (page == 1 && !Lineages.Contains(d.speciesLineage)) return "Обери різновид гнома.";
            if (page == 1 && Modern(d) && !new[] { 3, 4, 5 }.Contains(d.speciesMagicAbility)) return "Обери характеристику гном’ячої магії.";
            if (page == 3 && NeedsToolReplacement(d) && !ToolReplacements(d).Contains(d.originToolReplacement)) return "Обери заміну повторного володіння інструментами.";
            return null;
        }
        internal static void Bonuses(CharacterCreationDraft d, int[] scores)
        {
            if (!Gnome(d) || Modern(d)) return;
            scores[3] += 2;
            if (d.speciesLineage == "forest") scores[1]++;
            if (d.speciesLineage == "rock") scores[2]++;
        }
        public static string Features(CharacterCreationDraft d)
        {
            string result = "\nГном: Малий розмір, темнозір 60 футів.\n" + (Modern(d)
                ? "Гном’яча хитрість: перевага на ряткидки Інтелекту, Мудрості й Харизми."
                : "Гном’яча хитрість: перевага на ряткидки Інтелекту, Мудрості й Харизми проти магії.");
            if (d.speciesLineage == "forest" && !Modern(d)) result += "\nРозмова з малими звірами: звуками й жестами передавай прості ідеї Малим і меншим звірам.";
            if(d.speciesLineage=="forest"&&Modern(d))result+="\n"+CharacterDescriptionText.Get("gnome.forest24");
            if (d.speciesLineage == "rock") result += Modern(d)
                ? "\nМайстрування гнома: за 10 хв накладання Фокусів створи крихітний механізм з одним ефектом спела; активація дотиком і бонусною дією. КО 5, 1 ОЗ; до 3 пристроїв, кожен розпадається за 8 год."
                : "\nЗнання винахідника: подвійна майстерність до Історії магічних речей, алхімічних об’єктів і технологій.\nМайстрування гнома: володіння інструментами ремонтника. За 1 год і 10 зм створи іграшку, запальничку чи музичну скриньку; КО 5, 1 ОЗ, до 3 пристроїв. Працює 24 год; 1 год ремонту подовжує роботу. Розбирання дією повертає матеріали.";
            if (!Modern(d)) result += "\nМалий розмір: атаки важкою зброєю із завадою.";
            return result;
        }
    }
}
