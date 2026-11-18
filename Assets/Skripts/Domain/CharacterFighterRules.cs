using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterFighterRules
    {
        public static readonly string[] Classic = { "champion", "battlemaster", "eldritch" };
        public static readonly string[] Modern = { "champion", "battlemaster", "eldritch", "psiwarrior" };
        public static readonly string[] Names = { "Чемпіон", "Майстер бою", "Містичний лицар", "Псі-воїн" };
        public static readonly string[] StyleNames = { "Захист", "Дуелі", "Стрільба", "Велика зброя", "Оборона союзника", "Бій двома зброями", "Бій наосліп", "Перехоплення", "Метальна зброя", "Беззбройний бій" };
        public static int[] StyleOptions(CharacterCreationDraft d) => Enumerable.Range(0, Revised(d) ? 10 : 6).ToArray();
        public static bool Is(CharacterCreationDraft d, string subclass) => d.characterClass == "fighter" && d.level >= 3 && d.subclass == subclass;
        internal static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static string[] Options(CharacterCreationDraft d) => Revised(d) ? Modern : Classic;
        public static bool Caster(CharacterCreationDraft d) => Is(d, "eldritch");
        public static int WindUses(CharacterCreationDraft d) => Revised(d) ? d.level >= 10 ? 4 : d.level >= 4 ? 3 : 2 : 1;
        internal static int Masteries(CharacterCreationDraft d) => d.level >= 16 ? 6 : d.level >= 10 ? 5 : d.level >= 4 ? 4 : 3;
        public static int Attacks(CharacterCreationDraft d) => d.level >= 20 ? 4 : d.level >= 11 ? 3 : d.level >= 5 ? 2 : 1;
        public static int SurgeUses(CharacterCreationDraft d) => d.level >= 17 ? 2 : d.level >= 2 ? 1 : 0;
        public static int IndomitableUses(CharacterCreationDraft d) => d.level >= 17 ? 3 : d.level >= 13 ? 2 : d.level >= 9 ? 1 : 0;
        public static int ManeuverCount(CharacterCreationDraft d) => !Is(d, "battlemaster") ? 0 : d.level >= 15 ? 9 : d.level >= 10 ? 7 : d.level >= 7 ? 5 : 3;
        public static int SuperiorityCount(CharacterCreationDraft d) => d.level >= 15 ? 6 : d.level >= 7 ? 5 : 4;
        public static int SuperiorityDie(CharacterCreationDraft d) => d.level >= 18 ? 12 : d.level >= 10 ? 10 : 8;
        public static int PsiCount(CharacterCreationDraft d) => d.level >= 17 ? 12 : d.level >= 13 ? 10 : d.level >= 9 ? 8 : d.level >= 5 ? 6 : 4;
        public static int PsiDie(CharacterCreationDraft d) => d.level >= 17 ? 12 : d.level >= 11 ? 10 : d.level >= 5 ? 8 : 6;
        public static bool SecondStyle(CharacterCreationDraft d) => Is(d, "champion") && d.level >= (Revised(d) ? 7 : 10);
        public static bool HasStyle(CharacterCreationDraft d, FighterStyle style) => d.fightingStyle == style || SecondStyle(d) && d.fighterSecondStyle == (int)style;
        internal static int SpellCount(CharacterCreationDraft d) => !Caster(d) ? 0 : new[] { 0, 0, 3, 4, 4, 4, 5, 6, 6, 7, 8, 8, 9, 10, 10, 11, 11, 11, 12, 13 }[Math.Clamp(d.level, 1, 20) - 1];
        internal static int Cantrips(CharacterCreationDraft d) => !Caster(d) ? 0 : d.level >= 10 ? 3 : 2;
        internal static int Slots(CharacterCreationDraft d, int circle)
        {
            if (!Caster(d)) return 0;
            int level = d.level;
            return circle == 1 ? level >= 7 ? 4 : level >= 4 ? 3 : 2 : circle == 2 && level >= 7 ? level >= 10 ? 3 : 2 : circle == 3 && level >= 13 ? level >= 16 ? 3 : 2 : circle == 4 && level >= 19 ? 1 : 0;
        }
        internal static bool Athlete(CharacterCreationDraft d) => Is(d, "champion") && !Revised(d) && d.level >= 7;
        internal static int AthleteBonus(CharacterCreationDraft d, int skill, IEnumerable<int> trained) => Athlete(d) && CharacterCreationRules.SkillAbilities[skill] <= 2 && !trained.Contains(skill) ? (CharacterLegacySpeciesRules.Proficiency(d) + 1) / 2 : 0;
        public static int[] StudentSkills(CharacterCreationDraft d) => !Is(d, "battlemaster") || !Revised(d) ? Array.Empty<int>() :
            CharacterClassCatalog.Find("fighter").Skills(d.edition).Except(CharacterCreationRules.BackgroundSkills(d)).Except(d.classSkills).Except(CharacterSpeciesRules.GrantedSkills(d)).Except(CharacterOriginFeatRules.Skills(d)).ToArray();
        public static string ChoiceError(CharacterCreationDraft d)
        {
            if (!StyleOptions(d).Contains((int)d.fightingStyle)) return "Обери бойовий стиль.";
            if (SecondStyle(d) && (!StyleOptions(d).Contains(d.fighterSecondStyle) || d.fighterSecondStyle == (int)d.fightingStyle)) return "Обери другий бойовий стиль без повтору.";
            if (!Is(d, "battlemaster")) return null;
            if (!CharacterBackgroundRules.AllTools.Take(17).Contains(d.fighterTool)) return "Обери ремісничий інструмент Майстра бою.";
            if (Revised(d) && !StudentSkills(d).Contains(d.fighterSkill)) return "Обери додаткову навичку Майстра бою без повторів.";
            if (d.fighterManeuvers.Count != ManeuverCount(d) || d.fighterManeuvers.Distinct().Count() != ManeuverCount(d) || d.fighterManeuvers.Any(id => !CharacterFighterManeuvers.Options(d).Contains(id))) return "Обери всі маневри Майстра бою без повторів.";
            return null;
        }
        public const int GoldOnlyEquipment = 2;
        public const int GoldOnlyAmount = 155;
        public const string GoldOnlyDescription = "155 зм замість класового спорядження.";

        public static bool TakesStartingGold(CharacterCreationDraft d) =>
            d.characterClass == "fighter" && Revised(d) && d.equipment == GoldOnlyEquipment;

        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if (Revised(d) && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            if (Is(d, "battlemaster") && Revised(d) && d.fighterSkill >= 0) skills.Add(d.fighterSkill);
            int pb = CharacterLegacySpeciesRules.Proficiency(d);
            bool dwarf = CharacterSpeciesRules.IsDwarf(d);
            bool goldOnly = TakesStartingGold(d);
            int armor = d.equipment == 0 ? Revised(d) ? 16 : 18 :
                (goldOnly ? 10 : Revised(d) ? 12 : 11) + CharacterCreationRules.Modifier(scores[1]);
            int speed = dwarf && !Revised(d) ? 25 : 30;
            if (d.equipment == 0 && scores[0] < 13 && !(dwarf && !Revised(d))) speed -= 10;
            return new CreatedCharacterStats {
                scores = scores, skills = skills.Distinct().ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, CharacterCreationRules.Modifier(scores[2])),
                armorClass = armor + (!goldOnly && HasStyle(d, FighterStyle.Defense) ? 1 : 0),
                initiative = CharacterCreationRules.Modifier(scores[1]) + (Athlete(d) ? (pb + 1) / 2 : 0),
                speed = speed, passivePerception = 10 + CharacterCreationRules.Modifier(scores[4]) + (skills.Contains(11) ? pb : 0),
                secondWindUses = WindUses(d), speciesName = CharacterSpeciesRules.Name(d),
                backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "", equipment = CharacterCreationRules.Equipment(d),
                features = Features(d), proficiencies = "Усі обладунки, щити, проста й військова зброя. Ряткидки: Сила, Статура."
            };
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var ids = new List<string> { Revised(d) ? "wind24" : "wind14" };
            if (d.level >= 2) { ids.Add(Revised(d) ? "surge24" : "surge14"); if (Revised(d)) ids.Add("tacticalmind"); }
            if (d.level >= 5) { ids.Add("attacks"); if (Revised(d)) ids.Add("shift"); }
            if (d.level >= 9) { ids.Add(Revised(d) ? "indomitable24" : "indomitable14"); if (Revised(d)) ids.Add("tacticalmaster"); }
            if (Revised(d) && d.level >= 13) ids.Add("studied");
            if (Is(d, "champion")) {
                ids.Add(d.level >= 15 ? "critical18" : "critical19");
                if (Revised(d)) ids.Add("athlete24"); else if (d.level >= 7) ids.Add("athlete14");
                if (Revised(d) && d.level >= 10) ids.Add("heroicwarrior");
                if (d.level >= 18) ids.Add(Revised(d) ? "survivor24" : "survivor14");
            }
            if (Is(d, "battlemaster")) {
                ids.Add("superiority");
                if (d.level >= 7) ids.Add(Revised(d) ? "enemy24" : "enemy14");
                if (d.level >= 15) ids.Add(Revised(d) ? "relentless24" : "relentless14");
            }
            if (Caster(d)) {
                ids.Add(Revised(d) ? "bond24" : "bond14");
                if (d.level >= 7) ids.Add(Revised(d) ? "warmagic24" : "warmagic14");
                if (d.level >= 10) ids.Add("eldritchstrike");
                if (d.level >= 15) ids.Add("charge");
                if (d.level >= 18) ids.Add(Revised(d) ? "improvedmagic24" : "improvedmagic14");
            }
            if (Is(d, "psiwarrior")) {
                ids.AddRange(new[] { "psipool", "protect", "psistrike", "movement" });
                if (d.level >= 7) ids.AddRange(new[] { "leap", "thrust" });
                if (d.level >= 10) ids.Add("guarded"); if (d.level >= 15) ids.Add("bulwark"); if (d.level >= 18) ids.Add("telekinetic");
            }
            var lines = ids.Select(id => CharacterFighterText.Get(id)).ToList();
            if (Is(d, "battlemaster"))
            {
                lines.Add(Revised(d)
                    ? "Учень війни: володієш обраним набором ремісничих інструментів і додатковою навичкою зі списку Воїна."
                    : "Учень війни: володієш обраним набором ремісничих інструментів.");
                lines.Add("Бойова перевага: кісток 4, з 7 рівня 5, з 15 рівня 6. Розмір кістки к8, з 10 рівня к10, з 18 рівня к12.");
                lines.AddRange(d.fighterManeuvers.Where(id => CharacterFighterManeuvers.Options(d).Contains(id))
                    .Select(id => CharacterFighterText.Get("maneuver." + id + (Revised(d) ? ".24" : ".14"))));
            }
            lines.AddRange(Styles(d));
            if (CharacterSpeciesRules.IsDwarf(d))
            {
                if (Revised(d)) lines.Add(CharacterFighterText.Get("dwarf24"));
                else lines.Add(CharacterSpeciesRules.Dwarf2014Features(d).TrimStart('\n'));
            }
            if (Revised(d) && d.species == "human") lines.Add("Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.");
            return string.Join("\n", lines) + CharacterBackgroundRules.Features(d);
        }
        internal static IEnumerable<string> Styles(CharacterCreationDraft d)
        {
            if (HasStyle(d, FighterStyle.Defense)) yield return CharacterDescriptionText.Get("style.defense");
            if (HasStyle(d, FighterStyle.Dueling)) yield return CharacterDescriptionText.Get("style.dueling");
            if (HasStyle(d, FighterStyle.Archery)) yield return CharacterDescriptionText.Get("style.archery");
            if (HasStyle(d, FighterStyle.GreatWeapon)) yield return CharacterDescriptionText.Get(Revised(d)?"style.great.24":"style.great.14");
            foreach (int style in StyleOptions(d).Where(i => i >= 4 && HasStyle(d, (FighterStyle)i))) yield return CharacterFighterText.Get("style" + style + (style == 4 && !Revised(d) ? ".14" : ".24"));
        }
    }
}
