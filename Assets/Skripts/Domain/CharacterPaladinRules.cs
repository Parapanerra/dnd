using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterPaladinRules
    {
        public static readonly string[] Classic = { "devotion", "ancients", "vengeance" };
        public static readonly string[] Modern = { "devotion", "ancients", "vengeance", "glory" };
        public static readonly string[] Names = { "Клятва відданості", "Клятва Древніх", "Клятва помсти", "Клятва слави" };
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "paladin";
        public static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static bool Is(CharacterCreationDraft d, string oath) => Active(d) && d.level >= 3 && d.subclass == oath;
        public static string[] Options(CharacterCreationDraft d) => Revised(d) ? Modern : Classic;
        public static int[] StyleOptions(CharacterCreationDraft d) => Revised(d) ? Enumerable.Range(0, 11).ToArray() : new[] { 0, 1, 3, 4 };
        public static string StyleName(int id) => id == 10 ? "Благословенний воїн" : CharacterFighterRules.StyleNames[id];
        public static bool Blessed(CharacterCreationDraft d) => Active(d) && Revised(d) && d.level >= 2 && d.fightingStyle == FighterStyle.BlessedWarrior;
        public static int LayOnHands(CharacterCreationDraft d) => Math.Max(1, d.level) * 5;
        public static int ChannelUses(CharacterCreationDraft d) => !Active(d) || d.level < 3 ? 0 : Revised(d) ? d.level >= 11 ? 3 : 2 : 1;
        public static int AuraBonus(CharacterCreationDraft d) => !Active(d) || d.level < 6 ? 0 : Math.Max(1, CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[5]));
        internal static int AuraRange(CharacterCreationDraft d) => d.level >= 18 ? 30 : 10;
        internal static int Slots(CharacterCreationDraft d, int circle)
        {
            if (!Active(d) || d.level < (Revised(d) ? 1 : 2)) return 0;
            int level = d.level;
            switch (circle)
            {
                case 1: return level >= 5 ? 4 : level >= 3 ? 3 : 2;
                case 2: return level < 5 ? 0 : level >= 7 ? 3 : 2;
                case 3: return level < 9 ? 0 : level >= 11 ? 3 : 2;
                case 4: return level < 13 ? 0 : level >= 17 ? 3 : level >= 15 ? 2 : 1;
                case 5: return level < 17 ? 0 : level >= 19 ? 2 : 1;
                default: return 0;
            }
        }
        public static int MaximumCircle(CharacterCreationDraft d) => Enumerable.Range(1, 5).Where(i => Slots(d, i) > 0).DefaultIfEmpty(0).Max();
        internal static int SpellCount(CharacterCreationDraft d) => !Active(d) || MaximumCircle(d) == 0 ? 0 : Revised(d)
            ? new[] { 2, 3, 4, 5, 6, 6, 7, 7, 9, 9, 10, 10, 11, 11, 12, 12, 14, 14, 15, 15 }[Math.Clamp(d.level, 1, 20) - 1]
            : Math.Max(1, d.level / 2 + CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[5]));
        internal static string[] OathSpells(CharacterCreationDraft d)
        {
            if (!Active(d) || d.level < 3 || !Options(d).Contains(d.subclass)) return Array.Empty<string>();
            string[] spells;
            switch (d.subclass)
            {
                case "ancients": spells = new[] { "Ensnaring Strike", "Speak with Animals", "Moonbeam", "Misty Step", "Plant Growth", "Protection from Energy", "Ice Storm", "Stoneskin", "Commune with Nature", "Tree Stride" }; break;
                case "vengeance": spells = new[] { "Bane", "Hunter's Mark", "Hold Person", "Misty Step", "Haste", "Protection from Energy", "Banishment", "Dimension Door", "Hold Monster", "Scrying" }; break;
                case "glory": spells = new[] { "Guiding Bolt", "Heroism", "Enhance Ability", "Magic Weapon", "Haste", "Protection from Energy", "Compulsion", "Freedom of Movement", "Legend Lore", "Yolande's Regal Presence" }; break;
                default: spells = new[] { "Protection from Evil and Good", Revised(d) ? "Shield of Faith" : "Sanctuary", Revised(d) ? "Aid" : "Lesser Restoration", "Zone of Truth", "Beacon of Hope", "Dispel Magic", "Freedom of Movement", "Guardian of Faith", "Commune", "Flame Strike" }; break;
            }
            int pairs = d.level >= 17 ? 5 : d.level >= 13 ? 4 : d.level >= 9 ? 3 : d.level >= 5 ? 2 : 1;
            return spells.Take(pairs * 2).ToArray();
        }
        public static string[] AutomaticSpells(CharacterCreationDraft d) => OathSpells(d)
            .Concat(Active(d) && Revised(d) && d.level >= 2 ? new[] { "Divine Smite" } : Array.Empty<string>())
            .Concat(Active(d) && Revised(d) && d.level >= 5 ? new[] { "Find Steed" } : Array.Empty<string>()).Distinct().ToArray();
        public static string ChoiceError(CharacterCreationDraft d) => Active(d) && d.level >= 2 && !StyleOptions(d).Contains((int)d.fightingStyle) ? "Обери бойовий стиль паладина." : null;
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores = CharacterCreationRules.Scores(d); bool dwarf = CharacterSpeciesRules.IsDwarf(d);
            var skills = CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if (Revised(d) && d.species == "human" && d.humanSkill >= 0) skills.Add(d.humanSkill);
            bool armored = !Revised(d) || d.equipment == 0;
            int armor = armored ? 16 + (d.equipment == 0 ? 2 : 0) : 10 + CharacterCreationRules.Modifier(scores[1]);
            if (armored && d.level >= 2 && d.fightingStyle == FighterStyle.Defense) armor++;
            int speed = dwarf && !Revised(d) ? 25 : 30;
            if (armored && scores[0] < 13 && !(dwarf && !Revised(d))) speed -= 10;
            if (Is(d, "glory") && d.level >= 7) speed += 10;
            return new CreatedCharacterStats { scores = scores, skills = skills.Distinct().ToList(),
                hitPoints = CharacterCreationRules.ClassHitPoints(d, CharacterCreationRules.Modifier(scores[2])),
                armorClass = armor, initiative = CharacterCreationRules.Modifier(scores[1]), speed = speed,
                passivePerception = 10 + CharacterCreationRules.Modifier(scores[4]) + (skills.Contains(11) ? CharacterLegacySpeciesRules.Proficiency(d) : 0),
                speciesName = CharacterSpeciesRules.Name(d), backgroundName = CharacterBackgroundRules.Find(d)?.name ?? "",
                equipment = CharacterCreationRules.Equipment(d), features = Features(d) };
        }
        public static string Features(CharacterCreationDraft d)
        {
            string version = Revised(d) ? "24" : "14"; var ids = new List<string> { "hands" + version };
            if (!Revised(d)) ids.Add("sense14");
            if (d.level >= 2) ids.Add("smite" + version);
            if (d.level >= 3) { ids.Add("channel" + version); if (!Revised(d)) ids.Add("health14"); else ids.Add("sense24"); }
            if (d.level >= 5) { ids.Add("extraattack"); if (Revised(d)) ids.Add("steed24"); }
            if (d.level >= 6) ids.Add("aura" + version);
            if (Revised(d) && d.level >= 9) ids.Add("abjure24");
            if (d.level >= 10) ids.Add("courage");
            if (d.level >= 11) ids.Add("radiant" + version);
            if (d.level >= 14) ids.Add("touch" + version);
            if (d.level >= 18) ids.Add("aurarange");
            if (d.level >= 3 && Options(d).Contains(d.subclass))
            {
                ids.Add(d.subclass + ".tenets");
                ids.Add(d.subclass + ".3." + version);
                if (d.level >= 7) ids.Add(d.subclass + ".7." + version);
                if (d.level >= 15) ids.Add(d.subclass + ".15." + version);
                if (d.level >= 20) ids.Add(d.subclass + ".20." + version);
            }
            var lines = ids.Select(CharacterPaladinText.Get).ToList();
            if (d.level >= 2) {
                if (Blessed(d)) lines.Add(CharacterPaladinText.Get("blessed"));
                else lines.AddRange(CharacterFighterRules.Styles(d));
            }
            if (CharacterSpeciesRules.IsDwarf(d)) lines.Add(Revised(d) ? CharacterFighterText.Get("dwarf24") : CharacterSpeciesRules.Dwarf2014Features(d).TrimStart('\n'));
            if (Revised(d) && d.species == "human") lines.Add("Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.");
            if (Revised(d)) lines.Add("Майстерність зброї: " + string.Join(", ", d.weaponMasteries.Where(i => i >= 0 && i < CharacterCreationRules.MasteryWeapons.Count).Select(i => CharacterCreationRules.MasteryWeapons[i])) + ".");
            return string.Join("\n", lines) + CharacterBackgroundRules.Features(d);
        }
    }
}
