using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterElfRules
    {
        public static readonly string[] Lineages = { "high", "wood", "drow" };
        public static readonly string[] LineageNames = { "Високий ельф", "Лісовий ельф", "Дроу" };
        public static readonly int[] KeenSkills = { 6, 11, 17 };
        public static bool Elf(CharacterCreationDraft d) => d.species == "elf";
        public static bool Modern(CharacterCreationDraft d) => Elf(d) && d.edition == RulesEdition.Rules2024;
        internal static string LineageName(CharacterCreationDraft d)
        {
            int index = Array.IndexOf(Lineages, d.speciesLineage);
            return index >= 0 ? LineageNames[index] : "";
        }
        internal static int MagicAbility(CharacterCreationDraft d) => Modern(d) ? d.speciesMagicAbility : d.speciesLineage == "drow" ? 5 : 3;
        internal static int[] Skills(CharacterCreationDraft d) => !Elf(d) ? Array.Empty<int>() :
            !Modern(d) ? new[] { 11 } : KeenSkills.Contains(d.speciesSkill) ? new[] { d.speciesSkill } : Array.Empty<int>();
        internal static string FreeSpell(CharacterCreationDraft d) => !Elf(d) || d.TotalLevel < 3 ? "" :
            d.speciesLineage == "drow" ? "Faerie Fire" : Modern(d) ? d.speciesLineage == "high" ? "Detect Magic" : d.speciesLineage == "wood" ? "Longstrider" : "" : "";
        internal static string[] Spells(CharacterCreationDraft d)
        {
            if (!Elf(d)) return Array.Empty<string>();
            string cantrip = d.speciesLineage == "high" ? d.speciesCantrip : d.speciesLineage == "drow" ? "Dancing Lights" : Modern(d) ? "Druidcraft" : "";
            return new[] { cantrip, FreeSpell(d) }.Where(name => !string.IsNullOrEmpty(name)).ToArray();
        }
        internal static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Elf(d)) return null;
            if (page == 1)
            {
                if (!Lineages.Contains(d.speciesLineage)) return "Обери ельфійський різновид.";
                if (Modern(d) && !new[] { 3, 4, 5 }.Contains(d.speciesMagicAbility)) return "Обери характеристику ельфійської магії.";
                if (d.speciesLineage == "high" && string.IsNullOrEmpty(d.speciesCantrip)) return "Обери заговір високого ельфа.";
            }
            if (page == 3)
            {
                if (Modern(d) && !KeenSkills.Contains(d.speciesSkill)) return "Обери навичку Гострих чуттів.";
                if (!Modern(d) && d.speciesLineage == "high" && (string.IsNullOrEmpty(d.speciesLanguage) || d.speciesLanguage == "Загальна" || d.speciesLanguage == "Ельфійська")) return "Обери додаткову мову високого ельфа.";
            }
            return null;
        }
        internal static void Bonuses(CharacterCreationDraft d, int[] scores)
        {
            if (!Elf(d) || Modern(d)) return;
            scores[1] += 2;
            if (d.speciesLineage == "high") scores[3]++;
            if (d.speciesLineage == "wood") scores[4]++;
            if (d.speciesLineage == "drow") scores[5]++;
        }
        internal static string Features(CharacterCreationDraft d)
        {
            string result="\n"+CharacterDescriptionText.Get(Modern(d)?"elf.fey24":"elf.fey14")+"\n"+CharacterDescriptionText.Get(Modern(d)?"elf.trance24":"elf.trance14");
            result += d.speciesLineage == "drow" ? "\nТемнозір 120 футів." : "\nТемнозір 60 футів.";
            if (!Modern(d) && d.speciesLineage == "drow") result += "\nЧутливість до сонця: завада на атаки й Уважність зором, якщо ти або ціль у прямому сонячному світлі.";
            if (!Modern(d) && d.speciesLineage == "wood") result += "\nМаскування в дикій природі: можна ховатися за легким природним заслоном — листям, зливою, снігом або туманом.";
            if(Modern(d))result+="\n"+CharacterDescriptionText.Get(d.speciesLineage=="high"?"elf.high24":"lineage.magic24");
            else if(d.speciesLineage=="drow")result+="\n"+CharacterDescriptionText.Get("drow.magic14");
            return result;
        }
    }
}
