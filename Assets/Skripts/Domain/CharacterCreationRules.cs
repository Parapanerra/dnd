using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Class-specific level limits keep unfinished higher-level rules inaccessible.
    // All calculations are independent of Unity and of the sheet's layout.
    public static class CharacterCreationRules
    {
        public static readonly IReadOnlyList<string> AbilityNames = Array.AsReadOnly(new string[] { "Сила", "Спритність", "Статура", "Інтелект", "Мудрість", "Харизма" });
        public static readonly IReadOnlyList<string> SkillNames = Array.AsReadOnly(new string[] { "Акробатика", "Догляд тварин", "Магія", "Атлетика", "Обман", "Історія", "Аналіз поведінки", "Залякування", "Аналіз", "Медицина", "Природа", "Уважність", "Артистичність", "Переконливість", "Релігія", "Спритність рук", "Скритність", "Виживання" });
        public static readonly IReadOnlyList<int> SkillAbilities = Array.AsReadOnly(new int[] { 1, 4, 3, 0, 5, 3, 4, 5, 3, 4, 3, 4, 5, 5, 3, 1, 1, 4 });
        public static readonly int[] FighterSkills = { 1, 3, 5, 6, 7, 11, 17, 0 };
        public static readonly int[] BarbarianSkills = { 1, 3, 7, 10, 11, 17 };
        public static readonly int[] RogueSkills2014 = { 0, 3, 4, 6, 7, 8, 11, 12, 13, 15, 16 };
        public static readonly int[] RogueSkills2024 = { 0, 3, 4, 6, 7, 8, 11, 13, 15, 16 };
        public static readonly int[] MonkSkills = { 0, 3, 5, 6, 14, 16 };
        public static readonly string[] RangerEnemies = { "Аберації", "Звірі", "Небожителі", "Конструкти", "Дракони", "Елементалі", "Феї", "Породження", "Велетні", "Чудовиська", "Слизи", "Рослини", "Нежить", "Два види гуманоїдів" };
        public static readonly string[] RangerTerrains = { "Арктика", "Узбережжя", "Пустеля", "Ліс", "Луки", "Гори", "Болото", "Підзем’я" };
        public static readonly string[] MonkSimpleWeapons = { "Дубина", "Кинджал", "Велика дубина", "Ручна сокира", "Метальний спис", "Легкий молот", "Булава", "Бойовий посох", "Серп", "Спис", "Легкий арбалет", "Дротик", "Короткий лук", "Праща" };
        public static readonly string[] MonkTools = { "Алхімічне приладдя", "Приладдя пивовара", "Приладдя каліграфа", "Інструменти теслі", "Інструменти картографа", "Інструменти шевця", "Кухарське приладдя", "Інструменти склодува", "Інструменти ювеліра", "Інструменти шкіряра", "Інструменти каменяра", "Приладдя художника", "Інструменти гончара", "Інструменти коваля", "Інструменти лудильника", "Інструменти ткача", "Інструменти різьбяра", "Волинка", "Барабан", "Цимбали", "Флейта", "Ріг", "Лютня", "Ліра", "Флейта Пана", "Шалмей", "Віола" };
        public static string MonkWeaponDamage(string weapon)
        {
            var definition=CharacterWeaponCatalog.Find(weapon,RulesEdition.Rules2014);
            if(definition!=null)return definition.damage;
            switch (weapon)
            {
                case "Велика дубина": case "Легкий арбалет": return "1d8";
                case "Ручна сокира": case "Метальний спис": case "Булава": case "Бойовий посох": case "Спис": case "Короткий лук": return "1d6";
                default: return "1d4";
            }
        }
        // Append missing names; the first nineteen indices are already stored in saves.
        public static readonly IReadOnlyList<string> MasteryWeapons = Array.AsReadOnly(new[] { "Сокира дворучна", "Ручна сокира", "Довгий меч", "Дворучний меч", "Бойова сокира", "Булава", "Спис", "Кинджал", "Ціп", "Короткий меч", "Довгий лук", "Дротик", "Короткий лук", "Скімітар", "Рапіра", "Алебарда", "Бойовий молот", "Легкий молот", "Бойовий молот дворучний" }
            .Concat(CharacterWeaponCatalog.All.Where(w=>w.edition==RulesEdition.Rules2024&&w.mastery.Length>0).Select(w=>w.name)).Distinct().ToArray());
        private static readonly int[] BarbarianMasteryOptions = Enumerable.Range(0,MasteryWeapons.Count).Where(i=>!CharacterWeaponCatalog.Find(MasteryWeapons[i],RulesEdition.Rules2024).ranged).ToArray();
        private static readonly int[] RogueMasteryOptions = Enumerable.Range(0,MasteryWeapons.Count).Where(i=>{var w=CharacterWeaponCatalog.Find(MasteryWeapons[i],RulesEdition.Rules2024);return w.simple||w.finesse||w.light;}).ToArray();
        public static int[] MasteryOptions(CharacterCreationDraft d) => d.characterClass == "barbarian" ? (int[])BarbarianMasteryOptions.Clone() : d.characterClass == "rogue" ? (int[])RogueMasteryOptions.Clone() : Enumerable.Range(0, MasteryWeapons.Count).ToArray();
        public static int MasteryCount(CharacterCreationDraft d) => d.level < 1 ? 0 : d.characterClass == "fighter" ? CharacterFighterRules.Masteries(d) : d.characterClass == "barbarian" ? CharacterBarbarianRules.Masteries(d) : d.characterClass == "rogue" || d.characterClass == "paladin" || d.characterClass == "ranger" ? 2 : 0;
        public static int[] ClassSkillOptions(CharacterCreationDraft d) => (CharacterClassCatalog.Find(d.characterClass)?.Skills(d.edition) ?? Array.Empty<int>())
            .Where(x => !CharacterSpeciesRules.GrantedSkills(d).Contains(x) && !CharacterMonkSubclassRules.MercySkills(d).Contains(x) && (!CharacterClericRules.Classic(d) || d.subclass != "knowledge" || d.knowledgeSkills == null || !d.knowledgeSkills.Contains(x))).ToArray();
        public static int ClassSkillCount(CharacterCreationDraft d) => d.IsSecondaryTrack ? CharacterSecondaryClass.EntrySkills(d)
            : CharacterClassCatalog.Find(d.characterClass)?.skillCount ?? 0;
        public static int MaxSupportedLevel(CharacterCreationDraft d)
        {
            var definition = CharacterClassCatalog.Find(d.characterClass);
            return definition == null || d.edition == RulesEdition.Rules2024 && !definition.supports2024 ? 0 : definition.supportedLevel;
        }
        public static int HitDie(CharacterCreationDraft d) => CharacterClassCatalog.Find(d.characterClass)?.hitDie ?? 0;
        public static int FixedHitPoints(CharacterCreationDraft d) => HitDie(d) / 2 + 1;
        public static string RequiredSubclass(CharacterCreationDraft d) => d.level < 3 ? "" :
            d.characterClass == "barbarian" ? "berserker" : d.characterClass == "rogue" ? "thief" :
            d.characterClass == "monk" ? "openhand" : d.characterClass == "fighter" ? "champion" : "";
        public static bool HasSubclassChoice(CharacterCreationDraft d) => CharacterWizardRules.Active(d) && d.level >= CharacterWizardRules.SchoolLevel(d) ? CharacterWizardRules.HasSchool(d) : CharacterWarlockRules.Active(d)&&d.level>=(CharacterWarlockRules.Modern(d)?3:1)?CharacterWarlockRules.PatronOptions(d).Contains(d.subclass): CharacterDruidRules.Active(d) && d.level >= CharacterDruidRules.SubclassLevel(d) ? CharacterDruidRules.Options(d).Contains(d.subclass) : CharacterClericRules.Active(d) && d.level >= CharacterClericRules.SubclassLevel(d) ? CharacterClericRules.Options(d).Contains(d.subclass) : CharacterBloodHunterRules.Active(d) && d.level >= 3 ? CharacterBloodHunterRules.Orders.Contains(d.subclass) : CharacterArtificerRules.Active(d) && d.level >= 3 ? CharacterArtificerRules.Options(d).Contains(d.subclass) : CharacterBardRules.Active(d) && d.level >= 3 ? CharacterBardRules.Options(d).Contains(d.subclass) : CharacterSorcererRules.Active(d) && d.level >= CharacterSorcererRules.SubclassLevel(d) ? CharacterSorcererRules.Options(d).Contains(d.subclass) : d.characterClass == "ranger" && d.level >= 3 ? CharacterRangerRules.Options(d).Contains(d.subclass) : d.characterClass == "paladin" && d.level >= 3 ? CharacterPaladinRules.Options(d).Contains(d.subclass) : d.characterClass == "fighter" && d.level >= 3 ? CharacterFighterRules.Options(d).Contains(d.subclass) : d.characterClass == "rogue" && d.level >= 3 ? CharacterRogueRules.Options(d).Contains(d.subclass) : d.characterClass == "barbarian" && d.level >= 3 ? CharacterBarbarianRules.Options(d).Contains(d.subclass) : d.characterClass == "monk" && d.level >= 3 ? CharacterMonkSubclassRules.Options(d).Contains(d.subclass) : string.IsNullOrEmpty(RequiredSubclass(d)) || d.subclass == RequiredSubclass(d);
        internal static int ClassHitPoints(CharacterCreationDraft d, int constitution)
        {
            int level = Math.Max(1, d.level);
            int die = HitDie(d);
            int speciesBonus = CharacterSpeciesRules.HitPointsPerLevel(d);
            var secondary = d.secondaryClass;
            if (secondary?.IsSelected != true || secondary.level < 1 || secondary.level > CharacterSecondaryClass.MaximumLevel)
                return HitPointsFromClassLevels(Enumerable.Repeat(die, level), d.hitPointChoices, constitution, speciesBonus);

            var dice = Enumerable.Repeat(die, level).Concat(Enumerable.Repeat(CharacterClassCatalog.Find(secondary.classId)?.hitDie ?? 0, secondary.level));
            var choices = Enumerable.Range(0, level - 1).Select(i => d.hitPointChoices?.ElementAtOrDefault(i) ?? 0)
                .Concat(Enumerable.Range(0, secondary.level).Select(i => secondary.hitPointChoices?.ElementAtOrDefault(i) ?? 0)).ToArray();
            return HitPointsFromClassLevels(dice, choices, constitution, speciesBonus);
        }

        // Hit dice follow the order in which the character took the levels. Only character level 1 grants a full die.
        internal static int HitPointsFromClassLevels(IEnumerable<int> hitDice, IReadOnlyList<int> choices, int constitution, int speciesBonus)
        {
            int total = 0;
            int level = 0;
            foreach (int die in hitDice)
            {
                int choice = level == 0 ? die : choices != null && choices.Count >= level ? choices[level - 1] : 0;
                int basePoints = choice == -1 ? die / 2 + 1 : choice;
                total += Math.Max(1, basePoints + constitution) + speciesBonus;
                level++;
            }
            return total;
        }
        public static readonly string[] MusicalInstruments = { "Волинка", "Барабан", "Цимбали", "Флейта", "Ріг", "Лютня", "Ліра", "Флейта Пана", "Шалмей", "Віола" };
        public static string[] BardWeapons => new[] { "Рапіра", "Довгий меч" }.Concat(MonkSimpleWeapons).ToArray();
        public static string[] DruidFirstItems => new[] { "Дерев’яний щит" }.Concat(MonkSimpleWeapons).ToArray();
        public static string[] DruidWeapons => new[] { "Скімітар" }.Concat(MonkSimpleWeapons.Take(10)).ToArray();
        public static int SpellChoiceCount(CharacterCreationDraft d)
        {
            switch (d.characterClass)
            {
                case "fighter": return CharacterFighterRules.SpellCount(d);
                case "rogue": return CharacterRogueRules.SpellCount(d);
                case "sorcerer": return CharacterSorcererRules.SpellCount(d);
                case "warlock": return CharacterWarlockRules.SpellCount(d);
                case "bloodhunter": return CharacterBloodHunterRules.SpellCount(d);
                case "artificer": return CharacterArtificerRules.SpellCount(d);
                case "bard": return CharacterBardRules.SpellCount(d);
                case "wizard": return CharacterWizardRules.Prepared(d);
                case "cleric": return CharacterClericRules.SpellCount(d);
                case "druid": return CharacterDruidRules.SpellCount(d);
                case "paladin": return CharacterPaladinRules.SpellCount(d);
                case "ranger": return CharacterRangerRules.SpellCount(d);
                default: return 0;
            }
        }
        public static int CantripCount(CharacterCreationDraft d)
        {
            switch (d.characterClass)
            {
                case "ranger": return CharacterRangerRules.Druidic(d) ? 2 : 0;
                case "paladin": return CharacterPaladinRules.Blessed(d) ? 2 : 0;
                case "fighter": return CharacterFighterRules.Cantrips(d);
                case "rogue": return CharacterRogueRules.Cantrips(d);
                case "bard": return CharacterBardRules.Cantrips(d);
                case "bloodhunter": return CharacterBloodHunterRules.Cantrips(d);
                case "artificer": return CharacterArtificerRules.Cantrips(d);
                case "warlock": return CharacterWarlockRules.Cantrips(d);
                case "sorcerer": return CharacterSorcererRules.Cantrips(d);
                case "wizard": return CharacterWizardRules.Cantrips(d);
                case "cleric": return CharacterClericRules.Cantrips(d);
                case "druid": return CharacterDruidRules.Cantrips(d);
                default: return 0;
            }
        }
        private static readonly int[] Costs = { 0, 1, 2, 3, 4, 5, 7, 9 };
        public static int Modifier(int score) => (int)Math.Floor((score - 10) / 2d);
        public static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
        public static int PointCost(int score) => score >= 8 && score <= 15 ? Costs[score - 8] : 1000;
        public static int[] OriginalBackgroundSkills(CharacterCreationDraft d) => CharacterBackgroundRules.Skills(d);
        public static int[] BackgroundSkills(CharacterCreationDraft d) => d.multiclassExistingSkills ?? OriginalBackgroundSkills(d)
            .Select(skill => CharacterSpeciesRules.GrantedSkills(d).Contains(skill) ? CharacterSpeciesRules.BackgroundReplacement(d,skill) : skill)
            .Where(skill => skill >= 0).ToArray();
        public static int[] AllowedIncreases(CharacterCreationDraft d) => CharacterBackgroundRules.Increases(d);

        public static int[] Scores(CharacterCreationDraft d,bool includeEquipment=true)
        {
            if (d.multiclassScores != null) return (int[])d.multiclassScores.Clone();
            int[] result = (int[])d.abilities.Clone();
            if (d.edition == RulesEdition.Rules2014) CharacterSpeciesRules.AddBonuses(d, result);
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
            CharacterLevelAdvancement.ApplyScores(d, result);
            if (includeEquipment)
            {
                CharacterArtificerEquipment.ApplyScores(d, result);
                if (d.secondaryClass?.classId == "artificer")
                    CharacterArtificerEquipment.ApplyScores(d.secondaryClass.EquipmentTrack(d), result);
            }
            return result;
        }

        public static void RepairDependentChoices(CharacterCreationDraft d)
        {
            CharacterOriginFeatRules.Repair(d);
            CharacterExpandedSpeciesRules.Repair(d);
            CharacterSpeciesRules.RepairLanguages(d);
            CharacterLevelAdvancement.EnsureArrays(d);
            int[] allowed = AllowedIncreases(d);
            if (!allowed.Contains(d.increaseTwo)) d.increaseTwo = -1;
            if (!allowed.Contains(d.increaseOne) || d.increaseOne == d.increaseTwo) d.increaseOne = -1;
            d.thirdIncrease = -1;
            CharacterLevelAdvancement.RepairChoices(d);
            if (!CharacterSpeciesRules.NeedsSkillReplacement(d) || !CharacterSpeciesRules.ReplacementOptions(d).Contains(d.originSkillReplacement)) d.originSkillReplacement = -1;
            if(CharacterSpeciesRules.RepeatedBackgroundSkills(d).Length<2||!CharacterSpeciesRules.ReplacementOptions(d).Contains(d.secondOriginSkillReplacement)||d.secondOriginSkillReplacement==d.originSkillReplacement)d.secondOriginSkillReplacement=-1;
            if (d.species != "human") d.humanSkill = -1;
            bool keepsLineage = d.species switch
            {
                "tiefling" or "goliath" or "elf" or "gnome" => true,
                "halfling" => d.edition == RulesEdition.Rules2014,
                "aasimarlegacy" => d.TotalLevel >= 3 && CharacterExpandedSpeciesRules.AasimarRevelations.Contains(d.speciesLineage),
                "genasi" => CharacterExpandedSpeciesRules.GenasiLineages.Contains(d.speciesLineage),
                "eladrin" => CharacterExpandedSpeciesRules.EladrinSeasons.Contains(d.speciesLineage),
                "shifter" => CharacterExpandedSpeciesRules.ShifterLineages.Contains(d.speciesLineage),
                "dragonbornftd" => CharacterFtdDragonbornRules.Families.Contains(d.speciesLineage),
                _ => false
            };
            if (!keepsLineage) d.speciesLineage = "";
            if (!CharacterGnomeRules.NeedsToolReplacement(d) || !CharacterGnomeRules.ToolReplacements(d).Contains(d.originToolReplacement)) d.originToolReplacement = "";
            if (d.species != "warforged") d.speciesTool = "";
            if (d.species == "warforged") { d.speciesCantrip = ""; d.speciesMagicAbility = -1; d.speciesLanguage = ""; }
            else if (d.species == "halfelf") { d.speciesCantrip = ""; d.speciesMagicAbility = -1; d.speciesSkill = -1; }
            else if (d.species == "tiefling") { d.speciesCantrip = ""; d.speciesSkill = -1; d.speciesLanguage = ""; if (d.edition == RulesEdition.Rules2014) d.speciesMagicAbility = -1; }
            else if (!CharacterElfRules.Elf(d) && !CharacterGnomeRules.Gnome(d))
            {
                d.speciesCantrip = ""; d.speciesMagicAbility = -1; d.speciesSkill = -1;
                if (d.species != "simic") d.speciesLanguage = "";
            }
            else if (CharacterGnomeRules.Gnome(d))
            {
                d.speciesCantrip = ""; d.speciesSkill = -1; d.speciesLanguage = "";
                if (!CharacterGnomeRules.Modern(d)) d.speciesMagicAbility = -1;
            }
            else
            {
                if (d.speciesLineage != "high") { d.speciesCantrip = ""; d.speciesLanguage = ""; }
                if (!CharacterElfRules.Modern(d)) { d.speciesMagicAbility = -1; d.speciesSkill = -1; }
                else d.speciesLanguage = "";
            }
            var background = BackgroundSkills(d);
            int[] options = ClassSkillOptions(d);
            d.classSkills.RemoveAll(x => background.Contains(x) || !options.Contains(x));
            if (d.expertise == null) d.expertise = new List<int>();
            CharacterBardRules.Repair(d);
            var proficient = background.Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Concat(CharacterRangerRules.ExpertiseOptions(d)).Concat(CharacterBardRules.ExtraSkills(d)).Concat(d.characterClass=="bard"&&d.species=="human"&&d.edition==RulesEdition.Rules2024&&d.humanSkill>=0?new[]{d.humanSkill}:Array.Empty<int>()).ToArray();
            d.expertise.RemoveAll(x => d.characterClass != "rogue" && CharacterRangerRules.ExpertiseCount(d) == 0 && CharacterBardRules.ExpertiseCount(d) == 0 ||
                (x != -1 || d.edition != RulesEdition.Rules2014 || d.characterClass != "rogue") && !proficient.Contains(x));
            if (d.weaponMasteries == null) d.weaponMasteries = new List<int>();
            d.weaponMasteries.RemoveAll(x => d.edition != RulesEdition.Rules2024 || x != -1 && !MasteryOptions(d).Contains(x));
            if (d.characterClass == "rogue" && d.expertise != null && d.expertise.Count > CharacterRogueRules.ExpertiseCount(d)) d.expertise.RemoveRange(CharacterRogueRules.ExpertiseCount(d), d.expertise.Count - CharacterRogueRules.ExpertiseCount(d));
            int requiredMasteries = d.edition == RulesEdition.Rules2024 ? MasteryCount(d) : 0;
            if (d.weaponMasteries.Count > requiredMasteries) d.weaponMasteries.RemoveRange(requiredMasteries, d.weaponMasteries.Count - requiredMasteries);
            if (d.preparedSpells == null) d.preparedSpells = new List<string>();
            if (SpellChoiceCount(d) == 0) d.preparedSpells.Clear();
            else if (d.preparedSpells.Count > SpellChoiceCount(d)) d.preparedSpells.RemoveRange(SpellChoiceCount(d), d.preparedSpells.Count - SpellChoiceCount(d));
            if (d.cantrips == null) d.cantrips = new List<string>();
            if (CantripCount(d) == 0) d.cantrips.Clear();
            else if (d.cantrips.Count > CantripCount(d)) d.cantrips.RemoveRange(CantripCount(d), d.cantrips.Count - CantripCount(d));
            if (d.bardInstruments == null) d.bardInstruments = new List<string>();
            if (d.characterClass != "bard") { d.bardInstruments.Clear(); d.bardWeapon = ""; d.bardPack = -1; d.bardCarriedInstrument = ""; }
            if (d.characterClass != "druid") { d.primalOrder = ""; d.druidFirstItem = ""; d.druidWeapon = ""; }
            CharacterRangerRules.Repair(d);
            CharacterSorcererRules.Repair(d);
            CharacterArtificerRules.Repair(d);
            CharacterBloodHunterRules.Repair(d);
            CharacterClericRules.Repair(d);
            CharacterDruidRules.Repair(d);
            CharacterWarlockRules.Repair(d);
            if (d.characterClass != "artificer") { d.artificerTool = ""; d.artificerWeapon = ""; d.artificerSecondWeapon = ""; d.artificerArmor = ""; }
            if (d.characterClass != "bloodhunter") { d.bloodAbility = -1; d.bloodCurse = ""; d.bloodWeaponMode = -1; d.bloodWeapon = ""; d.bloodSecondWeapon = ""; d.bloodArmor = ""; }
            if (d.characterClass != "sorcerer") { d.dragonAncestor = -1; d.sorcererWeapon = ""; d.sorcererFocus = ""; d.sorcererPack = -1; }
            if (!CharacterSorcererRules.Draconic(d)) d.dragonAncestor = -1;
            else if (d.language == "Драконяча") d.language = "";
            d.spellbookSpells ??= new List<string>();
            if (d.characterClass != "wizard") { d.spellbookSpells.Clear(); d.wizardWeapon = ""; d.wizardFocus = ""; d.wizardPack = -1; }
            CharacterWizardRules.Repair(d);
            if (CharacterWizardRules.Active(d))
                for (int i = 0; i < d.preparedSpells.Count; i++)
                    if (!CharacterWizardRules.Book(d).Contains(d.preparedSpells[i]) || CharacterWizardRules.Automatic(d).Contains(d.preparedSpells[i]))
                        d.preparedSpells[i] = "";
            if (d.characterClass != "barbarian" || d.edition != RulesEdition.Rules2024 ||
                !BarbarianSkills.Contains(d.primalKnowledgeSkill) || background.Contains(d.primalKnowledgeSkill) ||
                d.classSkills.Contains(d.primalKnowledgeSkill) || CharacterSpeciesRules.GrantedSkills(d).Contains(d.primalKnowledgeSkill)) d.primalKnowledgeSkill = -1;
            if (d.species == "human" && d.edition == RulesEdition.Rules2024 &&
                (background.Contains(d.humanSkill) || d.classSkills.Contains(d.humanSkill) || d.primalKnowledgeSkill == d.humanSkill))
                d.humanSkill = -1;
            if (d.characterClass != "rogue" || d.edition != RulesEdition.Rules2024 ||
                d.rogueLanguage == d.language || d.rogueLanguage == d.secondLanguage) d.rogueLanguage = "";
            if (d.characterClass != "ranger" || d.edition != RulesEdition.Rules2014)
            {
                d.rangerEnemy = ""; d.rangerHumanoids = ""; d.rangerTerrain = ""; d.rangerLanguage = "";
            }
        }

        public static List<string> Validate(CharacterCreationDraft d, bool requireName = true)
        {
            var errors = new List<string>();
            if (d == null) { errors.Add("Немає чернетки персонажа."); return errors; }
            if (MaxSupportedLevel(d) == 0) errors.Add("Обери клас персонажа.");
            if (d.level < 1 || d.level > MaxSupportedLevel(d)) errors.Add("Обери доступний рівень класу.");
            for (int hitLevel = 2; hitLevel <= Math.Min(d.level, 20); hitLevel++)
            {
                int choice = d.hitPointChoices != null && d.hitPointChoices.Length >= hitLevel - 1
                    ? d.hitPointChoices[hitLevel - 2] : 0;
                if (choice != -1 && (choice < 1 || choice > HitDie(d)))
                    errors.Add("Обери ОЗ для кожного рівня після першого: середнє або кістка класу.");
            }
            if (!HasSubclassChoice(d)) errors.Add("Обери підклас персонажа.");
            string barbarianError = CharacterBarbarianRules.ChoiceError(d);
            if (barbarianError != null) errors.Add(barbarianError);
            string monkError = CharacterMonkSubclassRules.ChoiceError(d);
            if (monkError != null) errors.Add(monkError);
            if (!Enum.IsDefined(typeof(RulesEdition), d.edition) || !d.IsSecondaryTrack && !Enum.IsDefined(typeof(AbilityMethod), d.abilityMethod) ||
                (d.characterClass == "fighter" && !Enum.IsDefined(typeof(FighterStyle), d.fightingStyle))) errors.Add("Невідомі правила або спосіб розрахунку.");
            if (requireName && (string.IsNullOrWhiteSpace(d.characterName) || d.characterName.Trim().Length > 80)) errors.Add("Введи ім’я персонажа: від 1 до 80 символів.");
            if (!d.IsSecondaryTrack)
            {
            CharacterOriginFeatRules.Repair(d);
            foreach (int page in new[] { 1, 4 }) { string error = CharacterOriginFeatRules.ChoiceError(d, page); if (error != null) errors.Add(error); }
            string speciesError = CharacterSpeciesRules.ChoiceError(d);
            if (speciesError != null) errors.Add(speciesError);
            string speciesSkillError = CharacterSpeciesRules.ChoiceError(d, 3);
            if (speciesSkillError != null) errors.Add(speciesSkillError);
            if (CharacterBackgroundRules.Find(d) == null) errors.Add("Обери передісторію.");
            foreach (int page in new[] { 3, 4 }) { string error = CharacterBackgroundRules.ChoiceError(d, page); if (error != null) errors.Add(error); }
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
            string advancementError = CharacterLevelAdvancement.ChoiceError(d);
            if (advancementError != null) errors.Add(advancementError);
            if (d.edition == RulesEdition.Rules2024)
            {
                int[] allowed = AllowedIncreases(d);
                if (d.bonusMode < 0 || allowed.Length != 3 || !d.threeIncreases &&
                    (!allowed.Contains(d.increaseTwo) || !allowed.Contains(d.increaseOne) || d.increaseTwo == d.increaseOne))
                    errors.Add("Обери бонуси характеристик передісторії.");
            }
            if (Scores(d,false).Where((score, ability) => score > CharacterLevelAdvancement.MaximumScore(d, ability)).Any()) errors.Add("Після бонусів характеристика не може перевищувати 20.");
            }
            int[] bg = BackgroundSkills(d);
            int requiredSkills = ClassSkillCount(d);
            if (d.classSkills == null || d.classSkills.Count != requiredSkills || d.classSkills.Distinct().Count() != requiredSkills || d.classSkills.Any(x => !ClassSkillOptions(d).Contains(x) || bg.Contains(x))) errors.Add("Обери навички класу без повторення передісторії.");
            if (d.characterClass == "rogue" && (d.expertise == null || d.expertise.Count != CharacterRogueRules.ExpertiseCount(d) || d.expertise.Distinct().Count() != CharacterRogueRules.ExpertiseCount(d) ||
                d.expertise.Any(x => x == -1 ? d.edition != RulesEdition.Rules2014 : !bg.Concat(d.classSkills ?? new List<int>()).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Contains(x))))
                errors.Add("Обери всі доступні експертизи плута.");
            if (d.characterClass == "barbarian" && d.edition == RulesEdition.Rules2024 && d.level >= 3 &&
                (!BarbarianSkills.Contains(d.primalKnowledgeSkill) || bg.Contains(d.primalKnowledgeSkill) || d.classSkills.Contains(d.primalKnowledgeSkill) || CharacterSpeciesRules.GrantedSkills(d).Contains(d.primalKnowledgeSkill))) errors.Add("Обери додаткову навичку Первісного знання.");
            if (d.species == "human" && d.edition == RulesEdition.Rules2024 && (d.humanSkill < 0 || d.humanSkill > 17 || CharacterBardRules.ExtraSkills(d).Contains(d.humanSkill) || bg.Contains(d.humanSkill) || (d.classSkills != null && d.classSkills.Contains(d.humanSkill)) || d.primalKnowledgeSkill == d.humanSkill)) errors.Add("Додаткова навичка людини має бути унікальною.");
            if (!d.IsSecondaryTrack && (d.equipment < 0 || d.equipment > 1 && !CharacterFighterRules.TakesStartingGold(d))) errors.Add("Обери набір спорядження.");
            if (!d.IsSecondaryTrack && d.edition == RulesEdition.Rules2024 && d.backgroundEquipmentChoice < 0) errors.Add("Обери спорядження або 50 зм передісторії.");
            if (d.characterClass == "fighter" && (int)d.fightingStyle < 0) errors.Add("Обери бойовий стиль.");
            if (CharacterBardRules.Active(d)) for (int page = 0; page <= 4; page++) { string error = CharacterBardRules.ChoiceError(d, page); if (error != null) errors.Add(error); }
            if (CharacterRangerRules.Active(d)) for (int page = 0; page <= 4; page++) { string error = CharacterRangerRules.ChoiceError(d, page); if (error != null) errors.Add(error); }
            if (d.characterClass == "paladin") { string error = CharacterPaladinRules.ChoiceError(d); if (error != null) errors.Add(error); }
            if (d.characterClass == "fighter") { string error = CharacterFighterRules.ChoiceError(d); if (error != null) errors.Add(error); }
            if (d.edition == RulesEdition.Rules2024 && MasteryCount(d) > 0 && (d.weaponMasteries == null || d.weaponMasteries.Count != MasteryCount(d) || d.weaponMasteries.Distinct().Count() != MasteryCount(d) || d.weaponMasteries.Any(x => !MasteryOptions(d).Contains(x))))
                errors.Add("Обери майстерності зброї класу.");
            int spellCount = SpellChoiceCount(d);
            if (spellCount > 0 && (d.preparedSpells == null || d.preparedSpells.Count != spellCount ||
                d.preparedSpells.Any(string.IsNullOrEmpty) || d.preparedSpells.Distinct().Count() != spellCount)) errors.Add("Обери всі заклинання класу без повторів.");
            int cantripCount = CantripCount(d);
            if (cantripCount > 0 && (d.cantrips == null || d.cantrips.Count != cantripCount ||
                d.cantrips.Any(string.IsNullOrEmpty) || d.cantrips.Distinct().Count() != cantripCount)) errors.Add("Обери всі заговори класу без повторів.");
            if (d.characterClass == "bard")
            {
                int instruments = d.IsSecondaryTrack ? 1 : 3;
                if (d.bardInstruments == null || d.bardInstruments.Count != instruments || d.bardInstruments.Distinct().Count() != instruments || d.bardInstruments.Any(x => !MusicalInstruments.Contains(x))) errors.Add(d.IsSecondaryTrack ? "Обери музичний інструмент у спорядженні." : "Обери три різні музичні інструменти.");
                if (!d.IsSecondaryTrack && (d.edition == RulesEdition.Rules2014 || d.equipment == 0) && !MusicalInstruments.Contains(d.bardCarriedInstrument)) errors.Add("Обери музичний інструмент у спорядженні.");
                if (!d.IsSecondaryTrack && d.edition == RulesEdition.Rules2014 && (!BardWeapons.Contains(d.bardWeapon) || d.bardPack < 0 || d.bardPack > 1)) errors.Add("Обери зброю та набір барда.");
            }
            if (d.characterClass == "druid")
            {
                if (d.edition == RulesEdition.Rules2024 && d.primalOrder != "magician" && d.primalOrder != "warden") errors.Add("Обери первісний шлях друїда.");
                if (!d.IsSecondaryTrack && d.edition == RulesEdition.Rules2014 && (!DruidFirstItems.Contains(d.druidFirstItem) || !DruidWeapons.Contains(d.druidWeapon))) errors.Add("Обери щит або зброю та основну зброю друїда.");
            }
            if (!d.IsSecondaryTrack && d.characterClass == "monk" && !MonkTools.Contains(d.monkTool)) errors.Add("Обери інструмент монаха.");
            if (!d.IsSecondaryTrack && d.characterClass == "monk" && d.edition == RulesEdition.Rules2014 && (d.monkPack < 0 || d.monkPack > 1)) errors.Add("Обери набір монаха.");
            if (!d.IsSecondaryTrack && d.characterClass == "monk" && d.edition == RulesEdition.Rules2014 && d.equipment == 1 && !MonkSimpleWeapons.Contains(d.monkWeapon)) errors.Add("Обери просту зброю монаха.");
            if (d.characterClass == "ranger" && d.edition == RulesEdition.Rules2014 &&
                (!RangerEnemies.Contains(d.rangerEnemy) || !RangerTerrains.Contains(d.rangerTerrain) ||
                 d.rangerEnemy == "Два види гуманоїдів" && d.rangerHumanoids.Split(',').Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().Count() != 2))
                errors.Add("Обери ворога й місцевість слідопита; для гуманоїдів запиши два різні види через кому.");
            if (!d.IsSecondaryTrack && CharacterSpeciesRules.NeedsExtraLanguage(d) && string.IsNullOrWhiteSpace(d.language)) errors.Add("Обери додаткову мову.");
            if (!d.IsSecondaryTrack && d.edition == RulesEdition.Rules2024 && (string.IsNullOrWhiteSpace(d.secondLanguage) || d.language == d.secondLanguage)) errors.Add("Обери дві різні мови крім Загальної.");
            if (d.characterClass == "rogue" && d.edition == RulesEdition.Rules2024 &&
                (string.IsNullOrWhiteSpace(d.rogueLanguage) || d.rogueLanguage == d.language || d.rogueLanguage == d.secondLanguage))
                errors.Add("Обери додаткову мову плута, відмінну від інших.");
            if (!d.IsSecondaryTrack)
            {
                if (d.edition == RulesEdition.Rules2014 && CharacterSpeciesRules.NeedsExtraLanguage(d) && d.language == "Загальна") errors.Add("Обери додаткову мову крім Загальної.");
                string[] additionalLanguages = CharacterSpeciesRules.AdditionalLanguages(d);
                if (additionalLanguages.Distinct().Count() != additionalLanguages.Length || additionalLanguages.Any(CharacterSpeciesRules.AutomaticLanguages(d).Contains))
                    errors.Add("Обери додаткові мови без повторів уже відомих.");
                if (string.IsNullOrWhiteSpace(d.alignment)) errors.Add("Обери світогляд.");
            }
            foreach (int page in new[] { 0, 3, 4 })
            {
                string druidError = CharacterDruidRules.ChoiceError(d, page); if(druidError!=null) errors.Add(druidError);
                string error = CharacterClericRules.ChoiceError(d, page);
                if (error != null) errors.Add(error);
                string sorcererError = CharacterSorcererRules.ChoiceError(d, page);
                if (sorcererError != null) errors.Add(sorcererError);
                string warlockError = CharacterWarlockRules.ChoiceError(d, page);
                if (warlockError != null) errors.Add(warlockError);
                string artificerError = CharacterArtificerRules.ChoiceError(d, page);
                if (artificerError != null) errors.Add(artificerError);
                string bloodError = CharacterBloodHunterRules.ChoiceError(d, page);
                if (bloodError != null) errors.Add(bloodError);
            }
            if (d.characterClass == "wizard")
            {
                if (d.spellbookSpells == null || d.spellbookSpells.Count != CharacterWizardRules.BookCount(d) || d.spellbookSpells.Any(string.IsNullOrEmpty) || d.spellbookSpells.Distinct().Count() != CharacterWizardRules.BookCount(d) || d.preparedSpells.Any(name => !CharacterWizardRules.Book(d).Contains(name) || CharacterWizardRules.Automatic(d).Contains(name))) errors.Add(CharacterWizardText.Get("book.error"));
                string wizardError = CharacterWizardRules.ChoiceError(d);
                if (wizardError != null) errors.Add(wizardError);
                if (!d.IsSecondaryTrack && d.edition == RulesEdition.Rules2014 && (d.wizardWeapon != "Бойовий посох" && d.wizardWeapon != "Кинджал" || d.wizardFocus != "Сумка компонентів" && d.wizardFocus != "Арканний фокус" || d.wizardPack < 0 || d.wizardPack > 1)) errors.Add("Обери зброю, фокус і набір чарівника.");
            }
            return errors;
        }

        public static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            CharacterOriginFeatRules.Repair(d);
            var result = CharacterNaturalArmor.Apply(d,CharacterLevelAdvancement.Apply(d, CharacterOriginFeatRules.Apply(d, CharacterSpeciesRules.Apply(d, CalculateClass(d)))));
            var secondary = d.secondaryClass?.IsSelected == true ? d.secondaryClass.ClassDraft(d) : null;
            if (secondary != null)
            {
                var additional = secondary.characterClass == "bloodhunter" && secondary.bloodAbility < 0
                    ? new CreatedCharacterStats { features = "", skills = new List<int>() } : CalculateClass(secondary);
                result.features += "\n" + additional.features;
                foreach (int skill in additional.skills.Concat(d.secondaryClass.BonusSkills(secondary)))
                    if (!result.skills.Contains(skill)) result.skills.Add(skill);
                if (additional.expertise != null || secondary.expertise.Count > 0)
                    result.expertise = (result.expertise ?? new List<int>()).Concat(additional.expertise ?? new List<int>())
                        .Concat(secondary.expertise).Where(skill => skill >= 0).Distinct().ToList();

                bool armor = CharacterNaturalArmor.StarterArmor(d) && CharacterNaturalArmor.WearsArmor(d) ||
                    CharacterArtificerEquipment.CreatedArmorBase(d) > 0;
                int shield = CharacterNaturalArmor.StarterShield(d);
                if (secondary.characterClass == "artificer")
                {
                    int baseArmor = CharacterArtificerEquipment.CreatedArmorBase(secondary);
                    int magicShield = CharacterArtificerEquipment.Bonus(secondary, "shield");
                    if (baseArmor > 0)
                        result.armorClass = Math.Max(result.armorClass, baseArmor + Math.Min(2, Modifier(result.scores[1])) + shield +
                            (d.species == "warforged" && d.edition == RulesEdition.Rules2014 ? 1 : 0));
                    result.armorClass += CharacterArtificerEquipment.WornArmorBonus(secondary) +
                        CharacterArtificerEquipment.ProtectionBonus(secondary) + (magicShield > 0 ? 2 + magicShield - shield : 0);
                }
                if (armor && HasDefenseStyle(secondary) && !HasDefenseStyle(d)) result.armorClass++;
                if (!armor && d.characterClass != "monk" && d.characterClass != "barbarian")
                {
                    int unarmored = 10 + Modifier(result.scores[1]);
                    if (secondary.characterClass == "barbarian")
                        result.armorClass = Math.Max(result.armorClass, unarmored + Modifier(result.scores[2]) + shield);
                    if (secondary.characterClass == "monk" && shield == 0)
                        result.armorClass = Math.Max(result.armorClass, unarmored + Modifier(result.scores[4]));
                }
                if (secondary.characterClass == "monk" && !armor && shield == 0)
                    result.speed += CharacterMonkRules.MovementBonus(secondary);
                if (secondary.characterClass == "barbarian" && secondary.level >= 5 &&
                    !(armor && CharacterNaturalArmor.HeavyStarter(d))) result.speed += 10;
            }
            result.SkillModifiers = Array.AsReadOnly(CharacterSkillRules.Calculate(d, secondary, result));
            result.passivePerception = 10 + result.SkillModifiers[11];
            return result;
        }

        private static bool HasDefenseStyle(CharacterCreationDraft d) => d.characterClass == "fighter"
            ? CharacterFighterRules.HasStyle(d, FighterStyle.Defense)
            : (d.characterClass == "paladin" || d.characterClass == "ranger" || d.characterClass == "bloodhunter") &&
                d.level >= 2 && d.fightingStyle == FighterStyle.Defense;

        private static CreatedCharacterStats CalculateClass(CharacterCreationDraft d)
        {
            if (d.characterClass == "barbarian") return CalculateBarbarian(d);
            if (d.characterClass == "rogue") return CalculateRogue(d);
            if (d.characterClass == "monk") return CalculateMonk(d);
            if (d.characterClass == "paladin") return CharacterPaladinRules.Calculate(d);
            if (d.characterClass == "ranger") return CharacterRangerRules.Calculate(d);
            if (d.characterClass == "bard") return CharacterBardRules.Calculate(d);
            if (d.characterClass == "druid") return CalculateDruid(d);
            if (d.characterClass == "cleric") return CharacterClericRules.Calculate(d);
            if (d.characterClass == "wizard") return CharacterWizardRules.Calculate(d);
            if (d.characterClass == "sorcerer") return CharacterSorcererRules.Calculate(d);
            if (d.characterClass == "warlock") return CharacterWarlockRules.Calculate(d);
            if (d.characterClass == "artificer") return CharacterArtificerRules.Calculate(d);
            if (d.characterClass == "bloodhunter") return CharacterBloodHunterRules.Calculate(d);
            return CharacterFighterRules.Calculate(d);
        }

        private static CreatedCharacterStats CalculateBarbarian(CharacterCreationDraft d) => CharacterBarbarianRules.Calculate(d);

        private static CreatedCharacterStats CalculateDruid(CharacterCreationDraft d)
        {
            int[] scores = Scores(d);
            bool modern = d.edition == RulesEdition.Rules2024;
            bool dwarf = CharacterSpeciesRules.IsDwarf(d);
            bool shield = modern ? d.equipment == 0 : d.druidFirstItem == "Дерев’яний щит" && d.druidWeapon != "Велика дубина";
            var skills = BackgroundSkills(d).Concat(d.classSkills).Distinct().ToList();
            if (modern && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            var result = new CreatedCharacterStats {
                scores = scores, skills = skills,
                hitPoints = ClassHitPoints(d, Modifier(scores[2])),
                armorClass = (modern && d.equipment == 1 ? 10 : 11) + Modifier(scores[1]) + (shield ? 2 : 0),
                initiative = Modifier(scores[1]) + 0,
                speed = dwarf && !modern ? 25 : 30,
                passivePerception = 10 + Modifier(scores[4]) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = dwarf ? modern ? "Дварф" : "Дварф (пагорбовий)" : "Людина",
                backgroundName = CharacterBackgroundRules.Find(d)?.name ?? ""
            };
            result.equipment = Equipment(d);
            result.features = CharacterDruidRules.Features(d);
            result.features += CharacterBackgroundRules.Features(d);
            if (dwarf) result.features += modern
                ? "\nТемнозір 120 футів; опір отруті та перевага проти стану Отруєний.\nМіцність дварфа: +1 ОЗ за рівень. Чуття каменю: треморчуття 60 футів, 10 хв, 2/довгий відпочинок на камені."
                : CharacterSpeciesRules.Dwarf2014Features(d);
            else if (modern && d.species == "human") result.features += "\nЛюдина: героїчне натхнення після довгого відпочинку; додаткова навичка.";
            return result;
        }

        private static CreatedCharacterStats CalculateMonk(CharacterCreationDraft d)
        {
            int[] s = Scores(d);
            bool modern = d.edition == RulesEdition.Rules2024;
            bool dwarf = CharacterSpeciesRules.IsDwarf(d);
            var skills = BackgroundSkills(d).Concat(d.classSkills).Concat(CharacterMonkSubclassRules.MercySkills(d)).Distinct().ToList();
            if (modern && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            var result = new CreatedCharacterStats {
                scores = s, skills = skills,
                hitPoints = ClassHitPoints(d, Modifier(s[2])),
                armorClass = 10 + Modifier(s[1]) + Modifier(s[4]),
                initiative = Modifier(s[1]) + 0,
                speed = (dwarf && !modern ? 25 : 30) + CharacterMonkRules.MovementBonus(d),
                passivePerception = 10 + Modifier(s[4]) + (skills.Contains(11) ? 2 : 0),
                speciesName = dwarf ? modern ? "Дварф" : "Дварф (пагорбовий)" : "Людина",
                backgroundName = CharacterBackgroundRules.Find(d)?.name ?? ""
            };
            result.equipment = Equipment(d);
            result.features = "Монах " + Math.Max(1, d.level) + " • D&D " + (modern ? "2024" : "2014") + "\n" + CharacterMonkRules.Features(d);
            string subclassFeatures = CharacterMonkSubclassRules.Features(d);
            if (subclassFeatures.Length > 0) result.features += "\n" + subclassFeatures;
            result.features += CharacterBackgroundRules.Features(d);
            if (dwarf) result.features += modern
                ? "\nТемнозір 120 футів; опір отруті та перевага проти стану Отруєний.\nМіцність дварфа: +1 ОЗ за рівень. Чуття каменю: треморчуття 60 футів, 10 хв, використань за бонусом майстерності за довгий відпочинок на камені."
                : CharacterSpeciesRules.Dwarf2014Features(d);
            else if (modern && d.species == "human") result.features += "\nЛюдина: героїчне натхнення після довгого відпочинку; додаткова навичка.";
            result.proficiencies = (modern ? "Проста зброя та легка військова зброя" : "Проста зброя, короткий меч") +
                "; без обладунків; " + d.monkTool + ". Ряткидки: Сила, Спритність.\nНавички: " +
                string.Join(", ", skills.Select(x => SkillNames[x])) + ".";
            return result;
        }

        private static CreatedCharacterStats CalculateRogue(CharacterCreationDraft d) => CharacterRogueRules.Calculate(d);

        // The existing kits/choices grant these weapons; this does not grant extra purchases.
        public static IEnumerable<string> StartingWeapons(CharacterCreationDraft d)
        {
            bool modern=d.edition==RulesEdition.Rules2024;
            if (CharacterFighterRules.TakesStartingGold(d) || modern && d.equipment != 0 && d.characterClass != "fighter") yield break;
            switch(d.characterClass)
            {
                case "artificer":yield break; // Infused and armor weapons use their dedicated writer.
                case "fighter":
                    if(modern){foreach(string w in d.equipment==0?new[]{"Дворучний меч","Ціп","Метальний спис"}:new[]{"Скімітар","Короткий меч","Довгий лук"})yield return w;}
                    else {yield return "Довгий меч";yield return "Ручна сокира";if(d.equipment==1)yield return "Довгий лук";}
                    break;
                case "barbarian":yield return modern||d.equipment==0?"Сокира дворучна":"Довгий меч";yield return "Ручна сокира";if(!modern)yield return "Метальний спис";break;
                case "rogue":yield return !modern&&d.equipment==0?"Рапіра":"Короткий меч";yield return "Кинджал";if(modern||d.equipment==0)yield return "Короткий лук";break;
                case "monk":yield return modern?"Спис":d.equipment==0?"Короткий меч":d.monkWeapon;yield return modern?"Кинджал":"Дротик";break;
                case "paladin":yield return "Довгий меч";yield return "Метальний спис";break;
                case "ranger":yield return "Довгий лук";yield return "Короткий меч";if(modern)yield return "Скімітар";break;
                case "cleric":yield return modern?"Булава":d.clericWeapon;if(!modern)yield return d.clericSecondWeapon;break;
                case "druid":yield return modern?"Серп":d.druidWeapon;if(modern)yield return "Бойовий посох";else if(d.druidFirstItem!="Дерев’яний щит")yield return d.druidFirstItem;break;
                case "bard":yield return modern?"Кинджал":d.bardWeapon;if(!modern)yield return "Кинджал";break;
                case "wizard":
                    if (modern) { yield return "Бойовий посох"; yield return "Кинджал"; }
                    else yield return d.wizardWeapon;
                    break;
                case "sorcerer":yield return modern?"Спис":d.sorcererWeapon;yield return "Кинджал";break;
                case "warlock":if(modern){yield return "Серп";yield return "Кинджал";}else{yield return d.warlockWeapon;yield return d.warlockSecondWeapon;yield return "Кинджал";}break;
                case "bloodhunter":yield return d.bloodWeapon;if(d.bloodWeaponMode==1)yield return d.bloodSecondWeapon;yield return "Легкий арбалет";break;
            }
        }
        internal static string Equipment(CharacterCreationDraft d)
        {
            if (d.characterClass == "cleric") return CharacterClericRules.Equipment(d);
            if (d.characterClass == "wizard") return CharacterWizardRules.Equipment(d);
            if (d.characterClass == "sorcerer") return CharacterSorcererRules.Equipment(d);
            if (d.characterClass == "warlock") return CharacterWarlockRules.Equipment(d);
            if (d.characterClass == "artificer") return CharacterArtificerRules.Equipment(d);
            if (d.characterClass == "bloodhunter") return CharacterBloodHunterRules.Equipment(d);
            if (d.characterClass == "druid") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Шкіряний обладунок; щит; серп; фокус друїда (посох); набір мандрівника; набір травника; 9 зм." : "50 зм замість класового спорядження."
                : d.druidFirstItem + "; " + d.druidWeapon + "; шкіряний обладунок; набір мандрівника; фокус друїда.";
            if (d.characterClass == "bard") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Шкіряний обладунок; 2 кинджали; " + d.bardCarriedInstrument + "; набір артиста; 19 зм." : "90 зм замість класового спорядження."
                : d.bardWeapon + "; шкіряний обладунок; кинджал; " + d.bardCarriedInstrument + "; " + (d.bardPack == 0 ? "набір дипломата." : "набір артиста.");
            if (d.characterClass == "paladin") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Кольчуга; щит; довгий меч; 6 метальних списів; священний символ; набір жерця; 9 зм." : "150 зм замість класового спорядження."
                : d.equipment == 0 ? "Кольчуга; довгий меч; щит; 5 метальних списів; набір жерця; священний символ."
                    : "Кольчуга; два довгі мечі; 5 метальних списів; набір мандрівника; священний символ.";
            if (d.characterClass == "ranger") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; фокус друїда; набір мандрівника; 7 зм." : "150 зм замість класового спорядження."
                : d.equipment == 0 ? "Лускатий обладунок; два короткі мечі; набір підземельника; довгий лук; сагайдак; 20 стріл."
                    : "Шкіряний обладунок; два короткі мечі; набір мандрівника; довгий лук; сагайдак; 20 стріл.";
            if (d.characterClass == "monk") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Спис; 5 кинджалів; " + d.monkTool + "; набір мандрівника; 11 зм."
                    : "50 зм замість класового спорядження."
                : (d.equipment == 0 ? "Короткий меч" : d.monkWeapon) +
                    (d.monkPack == 0 ? "; набір підземельника" : "; набір мандрівника") + "; 10 дротиків.";
            if (d.characterClass == "rogue") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Шкіряний обладунок; 2 кинджали; короткий меч; короткий лук; 20 стріл; сагайдак; злодійські інструменти; набір зломщика; 8 зм." : "100 зм замість класового спорядження."
                : d.equipment == 0 ? "Рапіра; короткий лук; 20 стріл; набір зломщика; шкіряний обладунок; 2 кинджали; злодійські інструменти." : "Короткий меч; ще один короткий меч; набір мандрівника; шкіряний обладунок; 2 кинджали; злодійські інструменти.";
            if (d.characterClass == "barbarian") return d.edition == RulesEdition.Rules2024
                ? d.equipment == 0 ? "Сокира дворучна; 4 ручні сокири; набір мандрівника; 15 зм." : "75 зм замість класового спорядження."
                : d.equipment == 0 ? "Сокира дворучна; 2 ручні сокири; набір мандрівника; 4 метальні списи." : "Довгий меч; 2 ручні сокири; набір мандрівника; 4 метальні списи.";
            bool modern = d.edition == RulesEdition.Rules2024;
            string kit = CharacterFighterRules.TakesStartingGold(d) ? CharacterFighterRules.GoldOnlyDescription :
                modern ? (d.equipment == 0 ? "Кольчуга; дворучний меч; ціп; 8 метальних списів; набір підземельника; 4 зм." : "Проклепана шкіра; скімітар; короткий меч; довгий лук; 20 стріл; сагайдак; набір підземельника; 11 зм.") :
                (d.equipment == 0 ? "Кольчуга; довгий меч; щит; 2 ручні сокири; набір підземельника." : "Шкіряний обладунок; довгий лук; 20 стріл; довгий меч; щит; 2 ручні сокири; набір підземельника.");
            string origin = CharacterBackgroundRules.Equipment(d, text => text);
            return kit + "\nПередісторія: " + origin;
        }
    }
}
