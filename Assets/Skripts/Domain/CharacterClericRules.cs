using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterClericRules
    {
        public static readonly string[] Domains = { "knowledge", "life", "light", "nature", "tempest", "trickery", "war" };
        public static readonly string[] DomainNames = { "Домен Знання", "Домен Життя", "Домен Світла", "Домен Природи", "Домен Бурі", "Домен Обману", "Домен Війни" };
        public static readonly int[] KnowledgeOptions = { 2, 5, 10, 14 };
        public static readonly int[] NatureOptions = { 1, 10, 17 };
        public static bool Classic(CharacterCreationDraft d) => d.characterClass == "cleric" && d.edition == RulesEdition.Rules2014;
        public static bool HeavyArmor(CharacterCreationDraft d) => Classic(d) ? new[] { "life", "nature", "tempest", "war" }.Contains(d.subclass) : d.divineOrder == "protector";
        public static bool MartialWeapons(CharacterCreationDraft d) => Classic(d) ? d.subclass == "tempest" || d.subclass == "war" : d.divineOrder == "protector";
        public static bool Warhammer(CharacterCreationDraft d) => MartialWeapons(d) || d.edition == RulesEdition.Rules2014 && CharacterSpeciesRules.IsDwarf(d);
        public static string[] Armors(CharacterCreationDraft d) => HeavyArmor(d) ? new[] { "Лускатий обладунок", "Шкіряний обладунок", "Кольчуга" } : new[] { "Лускатий обладунок", "Шкіряний обладунок" };

        internal static void Repair(CharacterCreationDraft d)
        {
            d.knowledgeSkills ??= new List<int>(); d.knowledgeLanguages ??= new List<string>();
            if (d.characterClass != "cleric")
            {
                d.clericStrike = "";
                d.divineOrder = ""; d.knowledgeSkills.Clear(); d.knowledgeLanguages.Clear(); d.natureSkill = -1;
                d.natureCantrip = ""; d.clericWeapon = ""; d.clericArmor = ""; d.clericSecondWeapon = ""; d.clericPack = -1;
                return;
            }
            if(d.level<SubclassLevel(d))d.subclass="";
            if(!Revised(d)||d.level<7)d.clericStrike="";
            if (!Classic(d) || d.subclass != "knowledge") { d.knowledgeSkills.Clear(); d.knowledgeLanguages.Clear(); }
            if (!Classic(d) || d.subclass != "nature") { d.natureSkill = -1; d.natureCantrip = ""; }
            if (!Armors(d).Contains(d.clericArmor)) d.clericArmor = "";
            if (!Warhammer(d) && d.clericWeapon == "Бойовий молот") d.clericWeapon = "";
            d.preparedSpells.RemoveAll(name => DomainSpells(d).Contains(name));
            if (Classic(d) && d.subclass == "light") d.cantrips.Remove("Light");
        }

        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (d.characterClass != "cleric") return null;
            bool classic = Classic(d);
            if (page == 0 && (classic ? !Domains.Contains(d.subclass) : d.divineOrder != "protector" && d.divineOrder != "thaumaturge"))
                return classic ? "Обери божественний домен жерця." : "Обери божественний порядок жерця.";
            if(page==0 && d.level>=SubclassLevel(d) && !Options(d).Contains(d.subclass))return "Обери божественний домен жерця.";
            if(page==0 && Revised(d) && d.level>=7 && d.clericStrike!="divine" && d.clericStrike!="potent")return Text("error.strike");
            if (page == 3 && classic)
            {
                if (d.subclass == "knowledge")
                {
                    var known = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills);
                    if (d.knowledgeSkills.Count != 2 || d.knowledgeSkills.Distinct().Count() != 2 || d.knowledgeSkills.Any(x => !KnowledgeOptions.Contains(x) || known.Contains(x))) return "Обери дві унікальні навички домену Знання.";
                    if (d.knowledgeLanguages.Count != 2 || d.knowledgeLanguages.Any(string.IsNullOrEmpty) || d.knowledgeLanguages.Distinct().Count() != 2 ||
                        d.knowledgeLanguages.Any(x => x == "Загальна" || x == d.language || x == d.speciesLanguage || x == CharacterSpeciesRules.FixedLanguage(d))) return "Обери дві різні додаткові мови домену Знання.";
                }
                if (d.subclass == "nature" && (!NatureOptions.Contains(d.natureSkill) || CharacterCreationRules.BackgroundSkills(d).Contains(d.natureSkill) || d.classSkills.Contains(d.natureSkill))) return "Обери навичку домену Природа.";
            }
            if (page == 4 && classic)
            {
                if (!d.IsSecondaryTrack && (!Armors(d).Contains(d.clericArmor) || d.clericWeapon != "Булава" && !(Warhammer(d) && d.clericWeapon == "Бойовий молот") ||
                    !CharacterCreationRules.MonkSimpleWeapons.Contains(d.clericSecondWeapon) || d.clericPack < 0 || d.clericPack > 1)) return "Обери зброю, обладунок і набір жерця.";
                if (d.subclass == "nature" && (string.IsNullOrEmpty(d.natureCantrip) || d.cantrips.Contains(d.natureCantrip))) return "Обери додатковий заговір друїда без повторів.";
            }
            return null;
        }

        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d);
            bool modern = d.edition == RulesEdition.Rules2024, dwarf = CharacterSpeciesRules.IsDwarf(d);
            int Mod(int ability) => CharacterCreationRules.Modifier(scores[ability]);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            var expertise = new List<int>();
            if (modern && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            if (!modern && d.subclass == "knowledge") { skills.AddRange(d.knowledgeSkills.Where(x => x >= 0)); expertise.AddRange(d.knowledgeSkills.Where(x => x >= 0)); }
            if (!modern && d.subclass == "nature" && d.natureSkill >= 0) skills.Add(d.natureSkill);
            int armor = modern ? d.equipment == 0 ? 15 + Math.Min(2, Mod(1)) : 10 + Mod(1) :
                d.clericArmor == "Кольчуга" ? 18 : d.clericArmor == "Лускатий обладунок" ? 16 + Math.Min(2, Mod(1)) : 13 + Mod(1);
            var result = new CreatedCharacterStats {
                scores = scores, skills = skills.Distinct().ToList(), expertise = expertise,
                hitPoints = CharacterCreationRules.ClassHitPoints(d,Mod(2)), armorClass = armor,
                initiative = Mod(1) + 0,
                speed = dwarf && !modern ? 25 : !modern && d.clericArmor == "Кольчуга" && scores[0] < 13 ? 20 : 30,
                passivePerception = 10 + Mod(4) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = dwarf ? modern ? "Дварф" : "Дварф (пагорбовий)" : "Людина",
                backgroundName = CharacterBackgroundRules.Find(d)?.name ?? ""
            };
            result.features = Features(d);
            result.features += CharacterBackgroundRules.Features(d);
            if (dwarf) result.features += modern
                ? "\nТемнозір 120 футів; опір отруті та перевага проти стану Отруєний.\nМіцність дварфа: +1 ОЗ за рівень. Чуття каменю: треморчуття 60 футів, 10 хв, 2/довгий відпочинок на камені."
                : CharacterSpeciesRules.Dwarf2014Features(d);
            else if (modern && d.species == "human") result.features += "\nЛюдина: героїчне натхнення після довгого відпочинку; додаткова навичка.";
            result.equipment = Equipment(d);
            return result;
        }

        internal static string DomainFeature(string domain)
        {
            switch (domain)
            {
                case "knowledge": return "Благословення знань: дві додаткові мови; дві навички зі списку Магія, Історія, Природа, Релігія з подвоєним бонусом майстерності.";
                case "life": return "Учень життя: лікувальні заклинання 1 рівня й вище відновлюють додатково 2 + рівень заклинання ОЗ.";
                case "light": return "Захисний спалах: реакцією дай заваду атаці видимого ворога в межах 30 футів; не діє на імунних до засліплення. Використання: модифікатор Мудрості, мінімум 1; відновлення після довгого відпочинку.";
                case "nature": return "Послушник природи: додатковий заговір друїда, одна навичка природи й важкі обладунки.";
                case "tempest": return "Гнів бурі: коли видимий ворог у межах 5 футів влучає по тобі, реакцією змусь його зробити ряткидок Спритності: 2к8 блискавичної чи громової шкоди, за успіху половина. Використання: модифікатор Мудрості, мінімум 1; довгий відпочинок.";
                case "trickery": return "Благословення ошуканця: дією торкнись іншої охочої істоти; вона має перевагу на Скритність протягом години або до наступного застосування.";
                case "war": return "Бойовий жрець: після дії Атака можна атакувати зброєю бонусною дією. Використання: модифікатор Мудрості, мінімум 1; відновлення після довгого відпочинку.";
                default: return "";
            }
        }

        internal static string Equipment(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024
            ? d.equipment == 0 ? "Кольчужна сорочка; щит; булава; священний символ; набір жерця; 7 зм." : "110 зм замість класового спорядження."
            : d.clericWeapon + "; " + d.clericArmor + "; " + d.clericSecondWeapon + (d.clericSecondWeapon == "Легкий арбалет" ? "; 20 болтів" : "") + "; щит; священний символ; " + (d.clericPack == 0 ? "набір жерця." : "набір мандрівника.");
    }
}
