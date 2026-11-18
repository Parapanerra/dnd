using System;

namespace Taruck.CharacterCreation
{
    using System.Linq;
    public static class CharacterSpeciesRules
    {
        public static readonly string[] Species = new[]{ "dwarf", "human", "halfling", "halforc", "orc", "mountaindwarf", "elf", "gnome", "dragonborn", "tiefling", "halfelf", "aasimar", "goliath", "warforged" }.Concat(CharacterExpandedSpeciesRules.Ids).ToArray();
        private static string[] BaseSpells(CharacterCreationDraft d) => CharacterElfRules.Elf(d) ? CharacterElfRules.Spells(d) : CharacterGnomeRules.Gnome(d) ? CharacterGnomeRules.Spells(d) : CharacterLegacySpeciesRules.Spells(d);
        public static string[] Spells(CharacterCreationDraft d) => BaseSpells(d).Concat(CharacterExpandedSpeciesRules.Spells(d)).Concat(new[] { HigherFreeSpell(d) }).Where(name => name.Length > 0).Distinct().ToArray();
        public static int MagicAbility(CharacterCreationDraft d) => d.species=="aasimarlegacy"?5: CharacterExpandedSpeciesRules.UsesMagic(d) ? d.expandedMagicAbility : CharacterElfRules.Elf(d) ? CharacterElfRules.MagicAbility(d) : CharacterGnomeRules.Gnome(d) ? CharacterGnomeRules.MagicAbility(d) : CharacterLegacySpeciesRules.MagicAbility(d);
        internal static string FreeSpell(CharacterCreationDraft d) => CharacterExpandedSpeciesRules.Active(d)?CharacterExpandedSpeciesRules.FreeSpells(d).FirstOrDefault()??"":CharacterElfRules.Elf(d) ? CharacterElfRules.FreeSpell(d) : CharacterGnomeRules.Gnome(d) ? CharacterGnomeRules.FreeSpell(d) : CharacterLegacySpeciesRules.FreeSpell(d);
        public static string[] FreeSpells(CharacterCreationDraft d) => new[] { FreeSpell(d), HigherFreeSpell(d) }.Where(name => name.Length > 0).Distinct().ToArray();
        internal static string HigherFreeSpell(CharacterCreationDraft d) => CharacterExpandedSpeciesRules.Active(d)?CharacterExpandedSpeciesRules.FreeSpells(d).Skip(1).FirstOrDefault()??"":d.TotalLevel < 5 ? "" : d.species == "elf"
            ? d.speciesLineage == "drow" ? "Darkness" : CharacterElfRules.Modern(d) ? d.speciesLineage == "high" ? "Misty Step" : "Pass without Trace" : ""
            : d.species == "tiefling" ? d.edition == RulesEdition.Rules2014 ? "Darkness" : d.speciesLineage == "abyssal" ? "Hold Person" : d.speciesLineage == "chthonic" ? "Ray of Enfeeblement" : "Darkness" : "";
        public static int FreeSpellUses(CharacterCreationDraft d) => CharacterGnomeRules.Modern(d) && d.speciesLineage == "forest" ? 2 + (Math.Max(1, d.TotalLevel) - 1) / 4 : 1;
        public static string[] Available(RulesEdition edition) => Species.Where(id => edition == RulesEdition.Rules2024 ? id != "halforc" && id != "mountaindwarf" && id != "halfelf" : id != "orc" && id != "aasimar" && id != "goliath" && !CharacterExpandedSpeciesRules.RevisedOnly(id)).ToArray();
        public static bool IsDwarf(CharacterCreationDraft d) => d.species == "dwarf" || d.species == "mountaindwarf";
        public static bool NeedsExtraLanguage(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024 || d.species == "human" || d.species == "warforged" || CharacterExpandedSpeciesRules.Active(d)&&!new[]{"leonin","locathah","loxodon","simic"}.Contains(d.species);
        public static int HitPointsPerLevel(CharacterCreationDraft d) => d.species == "dwarf" ? 1 : 0;
        internal static string Name(CharacterCreationDraft d) => Name(d.species, d.edition);
        public static string Name(string species, RulesEdition edition) => CharacterExpandedSpeciesRules.Ids.Contains(species)?CharacterExpandedSpeciesRules.Name(species):species == "warforged" ? "Кований" : species == "dragonborn" ? "Дракононароджений" : species == "tiefling" ? "Тифлінг" : species == "halfelf" ? "Напівельф" : species == "aasimar" ? "Асімар" : species == "goliath" ? "Голіаф" : species == "gnome" ? "Гном" : species == "elf" ? "Ельф" : species == "mountaindwarf" ? "Дварф (гірський)" : species == "halfling" ? "Напіврослик" : species == "halforc" ? "Напіворк" : species == "orc" ? "Орк" : species == "dwarf" ? edition == RulesEdition.Rules2014 ? "Дварф (пагорбовий)" : "Дварф" : species == "human" ? "Людина" : "";
        public static string LineageName(CharacterCreationDraft d) => CharacterFtdDragonbornRules.Active(d)?CharacterFtdDragonbornRules.LineageName(d): d.species=="aasimarlegacy"||d.species=="genasi"||d.species=="eladrin"||d.species=="shifter"?CharacterExpandedSpeciesRules.LineageName(d): new[] { "dragonborn", "tiefling", "goliath" }.Contains(d.species) ? CharacterLegacySpeciesRules.LineageName(d) : CharacterGnomeRules.Gnome(d) ? CharacterGnomeRules.LineageName(d) : CharacterElfRules.Elf(d)
            ? CharacterElfRules.LineageName(d)
            : d.species == "mountaindwarf" ? "Гірський" : d.species == "halfling" && d.edition == RulesEdition.Rules2014
            ? d.speciesLineage == "lightfoot" ? "Легконогий" : d.speciesLineage == "stout" ? "Кремезний" : "" : d.species == "dwarf" && d.edition == RulesEdition.Rules2014 ? "Пагорбовий" : "";
        public static string FixedLanguage(CharacterCreationDraft d) => d.species=="simic"?d.speciesLanguage:d.species=="vedalken"?"Ведалкенська":d.species=="locathah"?"Акван":d.species=="loxodon"?"Локсодонська":d.species=="verdan"?"Гоблінська":d.species=="leonin"?"Леонінська":d.species=="kalashtar"&&d.edition==RulesEdition.Rules2014?"Кворі":d.species == "dragonborn" ? "Драконяча" : d.species == "tiefling" ? "Інфернальна" : d.species == "halfelf" ? "Ельфійська" : CharacterGnomeRules.Gnome(d) ? "Гномська" : CharacterElfRules.Elf(d) ? "Ельфійська" : d.species == "aasimar" ? "Небесна" : d.species == "halforc" || d.species == "orc" ? "Оркська" : d.species == "halfling" ? "Галфлінгська" : IsDwarf(d) ? "Дварфська" : "";
        public static string[] AutomaticLanguages(CharacterCreationDraft d) => new[] { "Загальна", FixedLanguage(d) }
            .Concat(CharacterSorcererRules.Draconic(d) ? new[] { "Драконяча" } : Array.Empty<string>())
            .Concat(d.characterClass == "druid" ? new[] { "Друїдична" } : Array.Empty<string>())
            .Concat(d.characterClass == "rogue" ? new[] { "Злодійський жаргон" } : Array.Empty<string>())
            .Where(language => !string.IsNullOrEmpty(language)).Distinct().ToArray();
        public static string[] AdditionalLanguages(CharacterCreationDraft d) =>
            (NeedsExtraLanguage(d) ? d.edition == RulesEdition.Rules2024 ? new[] { d.language, d.secondLanguage } : new[] { d.language } : Array.Empty<string>())
            .Concat(CharacterLegacySpeciesRules.HalfElf(d) || CharacterElfRules.Elf(d) && !CharacterElfRules.Modern(d) && d.speciesLineage == "high" ? new[] { d.speciesLanguage } : Array.Empty<string>())
            .Concat(CharacterRangerRules.Languages(d))
            .Concat(d.characterClass == "rogue" && d.edition == RulesEdition.Rules2024 ? new[] { d.rogueLanguage } : Array.Empty<string>())
            .Concat(CharacterClericRules.Classic(d) && d.subclass == "knowledge" ? d.knowledgeLanguages ?? new System.Collections.Generic.List<string>() : Enumerable.Empty<string>())
            .Concat((d.backgroundLanguages ?? Array.Empty<string>()).Take(CharacterBackgroundRules.Find(d)?.languages ?? 0))
            .Where(language => !string.IsNullOrEmpty(language)).ToArray();
        public static string[] LanguageOptions(CharacterCreationDraft d, System.Collections.Generic.IEnumerable<string> candidates, string current) =>
            candidates.Except(d.multiclassExistingLanguages ?? AutomaticLanguages(d))
                .Where(language => language == current || !AdditionalLanguages(d).Contains(language)).Distinct().ToArray();
        internal static void RepairLanguages(CharacterCreationDraft d)
        {
            string[] known = AutomaticLanguages(d);
            if (known.Contains(d.language)) d.language = "";
            if (known.Contains(d.secondLanguage)) d.secondLanguage = "";
            if (d.species!="simic"&&known.Contains(d.speciesLanguage)) d.speciesLanguage = "";
            if (known.Contains(d.rangerLanguage)) d.rangerLanguage = "";
            if (known.Contains(d.rogueLanguage)) d.rogueLanguage = "";
            if (d.knowledgeLanguages != null) for (int i = 0; i < d.knowledgeLanguages.Count; i++) if (known.Contains(d.knowledgeLanguages[i])) d.knowledgeLanguages[i] = "";
            if (d.backgroundLanguages != null) for (int i = 0; i < d.backgroundLanguages.Length; i++) if (known.Contains(d.backgroundLanguages[i])) d.backgroundLanguages[i] = "";
        }
        public static int[] GrantedSkills(CharacterCreationDraft d) => CharacterExpandedSpeciesRules.Active(d)?CharacterExpandedSpeciesRules.Skills(d):d.species == "warforged" ? (d.speciesSkill >= 0 && d.speciesSkill < 18 ? new[] { d.speciesSkill } : Array.Empty<int>()) : d.species == "halfelf" ? (d.speciesSkills ?? Array.Empty<int>()).Where(i => i >= 0 && i < 18).Distinct().ToArray() : d.species == "halforc" ? new[] { 7 } : CharacterElfRules.Skills(d);
        internal static bool NeedsSkillReplacement(CharacterCreationDraft d) => CharacterCreationRules.OriginalBackgroundSkills(d).Any(GrantedSkills(d).Contains);
        public static int[] RepeatedBackgroundSkills(CharacterCreationDraft d)=>CharacterCreationRules.OriginalBackgroundSkills(d).Where(GrantedSkills(d).Contains).Distinct().ToArray();
        internal static int BackgroundReplacement(CharacterCreationDraft d,int skill)=>Array.IndexOf(RepeatedBackgroundSkills(d),skill)==0?d.originSkillReplacement:d.secondOriginSkillReplacement;
        public static int[] ReplacementOptions(CharacterCreationDraft d) => Enumerable.Range(0, 18).Except(CharacterCreationRules.OriginalBackgroundSkills(d)).Except(GrantedSkills(d)).ToArray();

        public static string ChoiceError(CharacterCreationDraft d, int page = 1)
        {
            if (page == 1 && !Available(d.edition).Contains(d.species)) return "Обери расу / вид персонажа.";
            if (page == 1 && d.species == "halfling" && d.edition == RulesEdition.Rules2014 && d.speciesLineage != "lightfoot" && d.speciesLineage != "stout") return "Обери різновид напіврослика.";
            string expandedError=CharacterExpandedSpeciesRules.ChoiceError(d,page);
            if(expandedError!=null)return expandedError;
            string elfError = CharacterElfRules.ChoiceError(d, page);
            if (elfError != null) return elfError;
            string gnomeError = CharacterGnomeRules.ChoiceError(d, page);
            if (gnomeError != null) return gnomeError;
            string otherError = CharacterLegacySpeciesRules.ChoiceError(d, page);
            if (otherError != null) return otherError;
            string warforgedError = CharacterWarforgedRules.ChoiceError(d, page);
            if (warforgedError != null) return warforgedError;
            if(page==3&&NeedsSkillReplacement(d))
            {
                int[] replacements=RepeatedBackgroundSkills(d).Select(skill=>BackgroundReplacement(d,skill)).ToArray();
                if(replacements.Any(i=>!ReplacementOptions(d).Contains(i)||d.classSkills.Contains(i))||replacements.Distinct().Count()!=replacements.Length)return "Обери заміну навички, яка повторюється в расі й передісторії.";
            }
            return null;
        }

        internal static void AddBonuses(CharacterCreationDraft d, int[] scores)
        {
            if (d.edition != RulesEdition.Rules2014) return;
            if(CharacterExpandedSpeciesRules.Active(d)){CharacterExpandedSpeciesRules.AddBonuses(d,scores);return;}
            if (CharacterElfRules.Elf(d)) { CharacterElfRules.Bonuses(d, scores); return; }
            if (CharacterGnomeRules.Gnome(d)) { CharacterGnomeRules.Bonuses(d, scores); return; }
            CharacterLegacySpeciesRules.Bonuses(d, scores);
            if (d.species == "warforged")
            {
                scores[2] += 2;
                if (d.speciesIncreases != null && d.speciesIncreases.Length > 0 && new[] { 0, 1, 3, 4, 5 }.Contains(d.speciesIncreases[0])) scores[d.speciesIncreases[0]]++;
            }
            if (d.species == "human") { for (int i = 0; i < 6; i++) scores[i]++; return; }
            if (IsDwarf(d)) { scores[2] += 2; if (d.species == "mountaindwarf") scores[0] += 2; else scores[4]++; return; }
            if (d.species == "halforc") { scores[0] += 2; scores[2]++; return; }
            if (d.species != "halfling") return;
            scores[1] += 2;
            if (d.speciesLineage == "lightfoot") scores[5]++;
            if (d.speciesLineage == "stout") scores[2]++;
        }

        internal static string Dwarf2014Features(CharacterCreationDraft d) => "\nТемнозір 60 футів; опір отруті та перевага проти отрути.\n" +
            (d.species == "mountaindwarf" ? "Дварфське навчання: володіння легкими й середніми обладунками. " : "Міцність дварфа: +1 ОЗ за рівень. ") +
            "Чуття каменю: подвійна майстерність до Історії кам’яної кладки.";

        internal static CreatedCharacterStats Apply(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            // Old class implementations carried different dwarf/human summaries.
            // Racial rules are recorded here once, independently of the chosen class.
            string[] oldPrefixes={"Дварф:","Темнозір 120 футів; опір отруті", "Темнозір 60 футів; опір отруті", "Міцність дварфа:","Дварфське навчання:","Чуття каменю:"};
            stats.features=string.Join("\n",stats.features.Split('\n').Where(line=>
                !(IsDwarf(d)&&oldPrefixes.Any(prefix=>line.StartsWith(prefix,StringComparison.Ordinal)))&&
                !(d.species=="human"&&line=="Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.")));
            if(IsDwarf(d))stats.features+="\n"+DwarfFeatures(d);
            if(d.species=="human"&&d.edition==RulesEdition.Rules2024)stats.features+="\n"+CharacterDescriptionText.Get("heroic.human");
            CharacterExpandedSpeciesRules.Apply(d,stats);
            stats.speciesName = Name(d);
            stats.skills = stats.skills.Concat(GrantedSkills(d)).Distinct().ToList();
            int proficiency=CharacterLegacySpeciesRules.Proficiency(d);
            stats.passivePerception = 10 + CharacterCreationRules.Modifier(stats.scores[4]) + (stats.expertise != null && stats.expertise.Contains(11) ? 2*proficiency : stats.skills.Contains(11) ? proficiency : 0);
            stats.features += CharacterLegacySpeciesRules.Features(d);
            if (d.species == "dragonborn" && d.edition == RulesEdition.Rules2024 && d.TotalLevel >= 5)
                stats.features += "\nДраконячий політ: бонусною дією спектральні крила на 10 хвилин або до Недієздатності; швидкість польоту дорівнює швидкості ходьби. Раз за довгий відпочинок.";
            if (CharacterWarforgedRules.Warforged(d)) { stats.armorClass++; stats.features += CharacterWarforgedRules.Features(d); }
            if (d.species == "goliath") stats.speed += 5;
            if (CharacterGnomeRules.Gnome(d))
            {
                if (!CharacterGnomeRules.Modern(d)) stats.speed -= 5;
                stats.features += CharacterGnomeRules.Features(d);
                return stats;
            }
            if (CharacterElfRules.Elf(d))
            {
                if (d.speciesLineage == "wood") stats.speed += 5;
                stats.passivePerception = 10 + CharacterCreationRules.Modifier(stats.scores[4]) + (stats.expertise != null && stats.expertise.Contains(11) ? 4 : stats.skills.Contains(11) ? 2 : 0);
                stats.features += CharacterElfRules.Features(d);
                return stats;
            }
            if (d.species == "halforc" || d.species == "orc")
            {
                stats.features += d.species == "halforc" ? "\nНапіворк: Середній розмір, темнозір 60 футів."
                    : "\nОрк: Середній розмір, темнозір 120 футів.\n"+CharacterDescriptionText.Get("orc.adrenaline");
                stats.features += "\nНевблаганна витривалість: якщо падаєш до 0 ОЗ, але не гинеш одразу, можеш залишитися з 1 ОЗ. Раз за довгий відпочинок.";
                if (d.species == "halforc") stats.features += "\nЛюті атаки: критичне влучання ближньою зброєю додає ще один кубик її шкоди.";
                return stats;
            }
            if (d.species != "halfling") return stats;
            bool modern = d.edition == RulesEdition.Rules2024;
            if (!modern) stats.speed -= 5; // Class calculations preserve armor penalties relative to 30 feet.
            stats.features += "\nНапіврослик: Малий розмір. Везіння: натуральну 1 на атаці, перевірці характеристики чи ряткидку можна перекинути; використовуй новий результат.\nХоробрість: перевага на ряткидки проти переляку. Спритність напіврослика: проходь крізь простір більших істот, але не зупиняйся в ньому.";
            if (modern || d.speciesLineage == "lightfoot") stats.features += "\nПриродна скритність: можна ховатися за істотою, більшою щонайменше на один розмір.";
            if (!modern && d.speciesLineage == "stout") stats.features += "\nСтійкість кремезного: перевага на ряткидки проти отрути та опір отруйній шкоді.";
            if (!modern) stats.features += "\nМалий розмір: атаки важкою зброєю із завадою.";
            return stats;
        }

        internal static string DwarfFeatures(CharacterCreationDraft d)
        {
            if(d.edition==RulesEdition.Rules2024)return string.Join("\n",CharacterDescriptionText.Get("dwarf.24"),CharacterDescriptionText.Get("dwarf.tough"),string.Format(CharacterDescriptionText.Get("dwarf.stone24"),CharacterLegacySpeciesRules.Proficiency(d)));
            return string.Join("\n",CharacterDescriptionText.Get("dwarf.14"),CharacterDescriptionText.Get("dwarf.stone14"),
                d.species=="mountaindwarf"?CharacterDescriptionText.Get("dwarf.training"):CharacterDescriptionText.Get("dwarf.tough"));
        }
    }
}
