using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterRangerRules
    {
        public static readonly string[] Classic = { "hunter", "beastmaster" };
        public static readonly string[] Modern = { "hunter", "beastmaster", "feywanderer", "gloomstalker" };
        public static readonly string[] Names = { "Мисливець", "Повелитель звірів", "Мандрівник фей", "Морокоблукач" };
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "ranger";
        public static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static bool Is(CharacterCreationDraft d, string id) => Active(d) && d.level >= 3 && d.subclass == id;
        public static string[] Options(CharacterCreationDraft d) => Revised(d) ? Modern : Classic;
        public static int[] StyleOptions(CharacterCreationDraft d) => Revised(d) ? Enumerable.Range(0, 10).Concat(new[] { 11 }).ToArray() : new[] { 0, 1, 2, 5 };
        public static string StyleName(int id) => id == 11 ? "Друїдичний воїн" : CharacterFighterRules.StyleNames[id];
        public static bool Druidic(CharacterCreationDraft d) => Active(d) && Revised(d) && d.level >= 2 && d.fightingStyle == FighterStyle.DruidicWarrior;
        public static int Wisdom(CharacterCreationDraft d) => CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[4]);
        public static int MarkUses(CharacterCreationDraft d) => !Active(d) || !Revised(d) ? 0 : d.level >= 17 ? 6 : d.level >= 13 ? 5 : d.level >= 9 ? 4 : d.level >= 5 ? 3 : 2;
        public static int ExpertiseCount(CharacterCreationDraft d) => !Active(d) || !Revised(d) || d.level < 2 ? 0 : d.level >= 9 ? 3 : 1;
        public static int EnemyCount(CharacterCreationDraft d) => Active(d) && !Revised(d) ? d.level >= 14 ? 3 : d.level >= 6 ? 2 : 1 : 0;
        public static int TerrainCount(CharacterCreationDraft d) => Active(d) && !Revised(d) ? d.level >= 10 ? 3 : d.level >= 6 ? 2 : 1 : 0;
        public static string[] Enemies(CharacterCreationDraft d) => new[] { d.rangerEnemy }.Concat(d.rangerExtraEnemies ?? Array.Empty<string>()).Take(EnemyCount(d)).ToArray();
        public static string[] Terrains(CharacterCreationDraft d) => new[] { d.rangerTerrain }.Concat(d.rangerExtraTerrains ?? Array.Empty<string>()).Take(TerrainCount(d)).ToArray();
        internal static string[] Languages(CharacterCreationDraft d) => !Active(d) ? Array.Empty<string>() : Revised(d)
            ? d.level >= 2 ? d.rangerDeftLanguages ?? Array.Empty<string>() : Array.Empty<string>()
            : new[] { d.rangerLanguage }.Concat(d.rangerExtraLanguages ?? Array.Empty<string>()).Take(EnemyCount(d)).ToArray();
        public static int[] FeySkills(CharacterCreationDraft d) => Is(d, "feywanderer") && d.rangerFeySkill >= 0 ? new[] { d.rangerFeySkill } : Array.Empty<int>();
        public static int[] ExpertiseOptions(CharacterCreationDraft d) => CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills)
            .Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Concat(FeySkills(d))
            .Concat(d.species == "human" && Revised(d) && d.humanSkill >= 0 ? new[] { d.humanSkill } : Array.Empty<int>()).Distinct().ToArray();
        public static int[] FeySkillOptions(CharacterCreationDraft d) => new[] { 4, 12, 13 }.Except(CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills)
            .Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Concat(new[] { d.humanSkill })).ToArray();
        internal static int Slots(CharacterCreationDraft d, int circle)
        {
            if (!Active(d) || d.level < (Revised(d) ? 1 : 2)) return 0;
            int level = d.level;
            switch (circle) {
                case 1: return level >= 5 ? 4 : level >= 3 ? 3 : 2;
                case 2: return level < 5 ? 0 : level >= 7 ? 3 : 2;
                case 3: return level < 9 ? 0 : level >= 11 ? 3 : 2;
                case 4: return level < 13 ? 0 : level >= 17 ? 3 : level >= 15 ? 2 : 1;
                case 5: return level < 17 ? 0 : level >= 19 ? 2 : 1;
                default: return 0;
            }
        }
        public static int MaximumCircle(CharacterCreationDraft d) => Enumerable.Range(1, 5).Where(i => Slots(d, i) > 0).DefaultIfEmpty(0).Max();
        internal static int SpellCount(CharacterCreationDraft d) => !Active(d) ? 0 : (Revised(d)
            ? new[] { 2, 3, 4, 5, 6, 6, 7, 7, 9, 9, 10, 10, 11, 11, 12, 12, 14, 14, 15, 15 }
            : new[] { 0, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11 })[Math.Clamp(d.level, 1, 20) - 1];
        public static string[] AutomaticSpells(CharacterCreationDraft d)
        {
            if (!Active(d) || !Revised(d)) return Array.Empty<string>();
            var spells = new List<string> { "Hunter's Mark" };
            string[] subclass = Is(d, "feywanderer") ? new[] { "Charm Person", "Misty Step", "Summon Fey", "Dimension Door", "Mislead" }
                : Is(d, "gloomstalker") ? new[] { "Disguise Self", "Rope Trick", "Fear", "Greater Invisibility", "Seeming" } : Array.Empty<string>();
            spells.AddRange(subclass.Take(d.level >= 17 ? 5 : d.level >= 13 ? 4 : d.level >= 9 ? 3 : d.level >= 5 ? 2 : 1));
            return spells.Distinct().ToArray();
        }
        public static string[] Prey(CharacterCreationDraft d) => Revised(d) ? new[] { "colossus", "horde" } : new[] { "colossus", "giant", "horde" };
        public static string[] Defense(CharacterCreationDraft d) => Revised(d) ? new[] { "escape", "multi" } : new[] { "escape", "multi", "steel" };
        public static string ChoiceError(CharacterCreationDraft d, int page)
        {
            if (!Active(d)) return null;
            if (page == 0) {
                if (!Revised(d) && (Enemies(d).Any(x => !CharacterCreationRules.RangerEnemies.Contains(x)) || Enemies(d).Where(x => x != CharacterCreationRules.RangerEnemies.Last()).Distinct().Count() != Enemies(d).Count(x => x != CharacterCreationRules.RangerEnemies.Last()) ||
                    Terrains(d).Any(x => !CharacterCreationRules.RangerTerrains.Contains(x)) || Terrains(d).Distinct().Count() != TerrainCount(d))) return CharacterRangerText.Get("error.travel");
                var humanoids = new[] { d.rangerHumanoids }.Concat(d.rangerExtraHumanoids ?? Array.Empty<string>()).ToArray();
                if (!Revised(d) && Enemies(d).Where((x, i) => x == CharacterCreationRules.RangerEnemies.Last() && (i >= humanoids.Length || (humanoids[i] ?? "").Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).Distinct().Count() != 2)).Any()) return CharacterRangerText.Get("error.travel");
                if (!Revised(d)) {
                    var kinds = Enemies(d).SelectMany((enemy, i) => enemy == CharacterCreationRules.RangerEnemies.Last() ? (humanoids[i] ?? "").Split(',').Select(x => x.Trim()).ToArray() : Array.Empty<string>()).ToArray();
                    if (kinds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != kinds.Length) return CharacterRangerText.Get("error.travel");
                }
                if (Is(d, "hunter") && (!Prey(d).Contains(d.rangerHunterPrey) || d.level >= 7 && !Defense(d).Contains(d.rangerHunterDefense) ||
                    !Revised(d) && (d.level >= 11 && !new[] { "volley", "whirlwind" }.Contains(d.rangerHunterMultiattack) || d.level >= 15 && !new[] { "evasion", "tide", "dodge" }.Contains(d.rangerHunterSuperior)))) return CharacterRangerText.Get("error.hunter");
            }
            if (page == 3) {
                int count = ExpertiseCount(d);
                if (count > 0 && (d.expertise.Count != count || d.expertise.Distinct().Count() != count || d.expertise.Any(x => !ExpertiseOptions(d).Contains(x)))) return CharacterRangerText.Get("error.expertise");
                if (Revised(d) && d.level >= 2 && (Languages(d).Length != 2 || Languages(d).Any(string.IsNullOrWhiteSpace))) return CharacterRangerText.Get("error.languages");
                if (Is(d, "feywanderer") && !FeySkillOptions(d).Contains(d.rangerFeySkill)) return CharacterRangerText.Get("error.feyskill");
            }
            if (page == 4 && d.level >= 2 && !StyleOptions(d).Contains((int)d.fightingStyle)) return CharacterRangerText.Get("error.style");
            return null;
        }
        internal static void Repair(CharacterCreationDraft d)
        {
            if (d.rangerExtraEnemies == null || d.rangerExtraEnemies.Length != 2) d.rangerExtraEnemies = new[] { "", "" };
            if (d.rangerExtraHumanoids == null || d.rangerExtraHumanoids.Length != 2) d.rangerExtraHumanoids = new[] { "", "" };
            if (d.rangerExtraTerrains == null || d.rangerExtraTerrains.Length != 2) d.rangerExtraTerrains = new[] { "", "" };
            if (d.rangerExtraLanguages == null || d.rangerExtraLanguages.Length != 2) d.rangerExtraLanguages = new[] { "", "" };
            if (d.rangerDeftLanguages == null || d.rangerDeftLanguages.Length != 2) d.rangerDeftLanguages = new[] { "", "" };
            if (!Is(d, "feywanderer") || !FeySkillOptions(d).Contains(d.rangerFeySkill)) d.rangerFeySkill = -1;
            if (Active(d)) {
                int n = ExpertiseCount(d); d.expertise.RemoveAll(x => !ExpertiseOptions(d).Contains(x));
                if (d.expertise.Count > n) d.expertise.RemoveRange(n, d.expertise.Count - n);
            }
            var known = CharacterSpeciesRules.AutomaticLanguages(d);
            foreach (var languages in new[] { d.rangerExtraLanguages, d.rangerDeftLanguages }) for (int i = 0; i < languages.Length; i++) if (known.Contains(languages[i])) languages[i] = "";
        }
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d); int dex = CharacterCreationRules.Modifier(scores[1]);
            bool armored = !Revised(d) || d.equipment == 0;
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Concat(FeySkills(d)).ToList();
            if (Revised(d) && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            return new CreatedCharacterStats { scores = scores, skills = skills.Distinct().ToList(), expertise = d.expertise.ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, CharacterCreationRules.Modifier(scores[2])),
                armorClass = (Revised(d) ? d.equipment == 0 ? 12 + dex : 10 + dex : d.equipment == 0 ? 14 + Math.Min(2, dex) : 11 + dex) + (armored && d.level >= 2 && d.fightingStyle == FighterStyle.Defense ? 1 : 0),
                initiative = dex + (Is(d, "gloomstalker") ? Wisdom(d) : 0),
                speed = (CharacterSpeciesRules.IsDwarf(d) && !Revised(d) ? 25 : 30) + (Revised(d) && d.level >= 6 ? 10 : 0),
                passivePerception = 10 + Wisdom(d) + (d.expertise.Contains(11) ? 2 : skills.Contains(11) ? 1 : 0) * CharacterLegacySpeciesRules.Proficiency(d),
                speciesName = CharacterSpeciesRules.Name(d), backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "", equipment = CharacterCreationRules.Equipment(d), features = Features(d) };
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var ids = new List<string>(); string v = Revised(d) ? "24" : "14";
            if (Revised(d)) {
                ids.Add("mark"); if (d.level >= 2) ids.Add("deft"); if (d.level >= 6) ids.Add("roving");
                if (d.level >= 10) ids.Add("tireless"); if (d.level >= 13) ids.Add("relentless"); if (d.level >= 14) ids.Add("veil");
                if (d.level >= 17) ids.Add("precise"); if (d.level >= 18) ids.Add("senses24"); if (d.level >= 20) ids.Add("slayer24");
            } else {
                ids.Add("enemy"); ids.Add("terrain"); if (d.level >= 3) ids.Add("primeval"); if (d.level >= 8) ids.Add("stride");
                if (d.level >= 10) ids.Add("hide"); if (d.level >= 14) ids.Add("vanish"); if (d.level >= 18) ids.Add("senses14"); if (d.level >= 20) ids.Add("slayer14");
            }
            if (d.level >= 5) ids.Add("extra");

            if (Is(d, "hunter")) {
                if (Revised(d)) ids.Add("lore"); ids.Add("prey." + d.rangerHunterPrey);
                if (d.level >= 7) ids.Add("defense." + d.rangerHunterDefense + (d.rangerHunterDefense == "multi" ? v : ""));
                if (d.level >= 11) ids.Add(Revised(d) ? "hunter11" : d.rangerHunterMultiattack);
                if (d.level >= 15) ids.Add(Revised(d) ? "hunter15" : d.rangerHunterSuperior);
            }
            if (Is(d, "beastmaster")) foreach (int level in new[] { 3, 7, 11, 15 }) if (d.level >= level) ids.Add("beast" + level + v);
            if (Is(d, "feywanderer")) {
                ids.Add(d.level >= 11 ? "dreadfey11" : "dreadfey3"); ids.Add("glamour");
                foreach (int level in new[] { 7, 11, 15 }) if (d.level >= level) ids.Add("fey" + level);
            }
            if (Is(d, "gloomstalker")) {
                ids.Add(d.level >= 11 ? "dreadgloom11" : "dreadgloom3"); ids.Add("umbral");
                foreach (int level in new[] { 7, 11, 15 }) if (d.level >= level) ids.Add("gloom" + level);
            }
            var lines = ids.Where(x => !string.IsNullOrEmpty(x) && !x.EndsWith(".")).Select(CharacterRangerText.Get).ToList();
            if (!Revised(d)) { lines.Add("Обрані вороги: " + string.Join(", ", Enemies(d).Select((enemy, i) => enemy == CharacterCreationRules.RangerEnemies.Last() ? i == 0 ? d.rangerHumanoids : d.rangerExtraHumanoids[i - 1] : enemy))); lines.Add("Обрані місцевості: " + string.Join(", ", Terrains(d))); }
            if (d.level >= 2) { if (Druidic(d)) lines.Add(CharacterRangerText.Get("druidic")); else lines.AddRange(CharacterFighterRules.Styles(d)); }
            if (CharacterSpeciesRules.IsDwarf(d)) lines.Add(Revised(d) ? CharacterFighterText.Get("dwarf24") : CharacterSpeciesRules.Dwarf2014Features(d).TrimStart('\n'));
            if (Revised(d) && d.species == "human") lines.Add("Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.");
            return string.Join("\n", lines) + CharacterBackgroundRules.Features(d);
        }
    }
}
