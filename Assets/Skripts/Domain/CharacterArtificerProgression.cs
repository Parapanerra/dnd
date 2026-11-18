using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // TCE and EFA tables are kept separate: EFA replaces infusions with item plans.
    public static partial class CharacterArtificerRules
    {
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "artificer";
        public static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static bool Is(CharacterCreationDraft d, string id) => Active(d) && d.level >= 3 && d.subclass == id && Options(d).Contains(id);
        public static string[] Options(CharacterCreationDraft d) => Revised(d)
            ? new[] { "alchemist", "armorer", "artillerist", "battlesmith", "cartographer" }
            : new[] { "alchemist", "armorer", "artillerist", "battlesmith" };
        public static string Name(string id) => CharacterArtificerText.Get("name." + id);
        internal static int Cantrips(CharacterCreationDraft d) => d.level >= 14 ? 4 : d.level >= 10 ? 3 : 2;
        public static int PlansKnown(CharacterCreationDraft d) => d.level < 2 ? 0 :
            (Revised(d) ? d.level >= 14 ? 7 : d.level >= 10 ? 6 : d.level >= 6 ? 5 : 4
             : d.level >= 18 ? 12 : d.level >= 14 ? 10 : d.level >= 10 ? 8 : d.level >= 6 ? 6 : 4)
             + (Revised(d) && d.level >= 18 ? 1 : 0)
             + (Revised(d) && Is(d, "armorer") && d.level >= 9 ? 1 : 0);
        public static int ItemLimit(CharacterCreationDraft d) => d.level < 2 ? 0 :
            (d.level >= 18 ? 6 : d.level >= 14 ? 5 : d.level >= 10 ? 4 : d.level >= 6 ? 3 : 2)
            + (Is(d, "armorer") && d.level >= 9 ? Revised(d) ? 1 : 2 : 0);
        internal static int AttunementLimit(CharacterCreationDraft d) => d.level >= 18 ? 6 : d.level >= 14 ? 5 : d.level >= 10 ? 4 : 3;
        internal static int Slots(CharacterCreationDraft d, int circle)
        {
            if (!Active(d)) return 0;
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
        public static int SpellCount(CharacterCreationDraft d) => Revised(d)
            ? new[] { 2, 3, 4, 5, 6, 6, 7, 7, 9, 9, 10, 10, 11, 11, 12, 12, 14, 14, 15, 15 }[Math.Clamp(d.level, 1, 20) - 1]
            : Math.Max(1, d.level / 2 + CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[3]));
        internal static int GeniusUses(CharacterCreationDraft d) => d.level < 7 ? 0 : Math.Max(1, CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[3]));
        public static int Elixirs(CharacterCreationDraft d) => !Is(d, "alchemist") ? 0 : Revised(d)
            ? d.level >= 15 ? 5 : d.level >= 9 ? 4 : d.level >= 5 ? 3 : 2
            : d.level >= 15 ? 3 : d.level >= 6 ? 2 : 1;
        public static string[] Models(CharacterCreationDraft d) => Revised(d) ? new[] { "guardian", "infiltrator", "dreadnaught" } : new[] { "guardian", "infiltrator" };
        public static string[] SubclassTools(CharacterCreationDraft d) => d.level < 3 ? Array.Empty<string>() :
            Is(d, "alchemist") ? Revised(d) ? new[] { "Алхімічне приладдя", "Набір травника" } : new[] { "Алхімічне приладдя" } :
            Is(d, "armorer") || Is(d, "battlesmith") ? new[] { "Інструменти коваля" } :
            Is(d, "artillerist") ? new[] { "Інструменти різьбяра" } :
            Is(d, "cartographer") ? new[] { "Приладдя каліграфа", "Інструменти картографа" } : Array.Empty<string>();
        internal static string[] BaseTools(CharacterCreationDraft d)
        {
            var owner = d.PrimaryCharacter;
            var tools = d.IsSecondaryTrack
                ? Revised(d) ? new[] { "Інструменти лудильника" } : new[] { "Злодійські інструменти", "Інструменти лудильника" }
                : new[] { "Злодійські інструменти", "Інструменти лудильника", d.artificerTool };
            return tools.Concat(CharacterBackgroundRules.Tools(owner)).Concat(CharacterOriginFeatRules.Tools(owner))
                .Concat(owner.species == "warforged" ? new[] { owner.speciesTool } : Array.Empty<string>())
                .Concat(CharacterSpeciesRules.IsDwarf(owner) && !Revised(d) ? new[] { owner.artisanTool } : Array.Empty<string>())
                .Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
        }
        public static int ToolReplacementCount(CharacterCreationDraft d) => SubclassTools(d).Count(BaseTools(d).Contains);
        public static string[] ToolReplacementOptions(CharacterCreationDraft d, int slot) => Tools.Except(BaseTools(d)).Except(SubclassTools(d))
            .Where(tool => !d.artificerToolReplacements.Where((_, i) => i != slot).Contains(tool)).ToArray();
        public static string[] GrantedTools(CharacterCreationDraft d) => new[] { "Злодійські інструменти", "Інструменти лудильника", d.artificerTool }
            .Concat(SubclassTools(d)).Concat(d.artificerToolReplacements).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();

        public static ArtificerPlanOption[] PlanOptions(CharacterCreationDraft d, int slot)
        {
            int year = Revised(d) ? 2024 : 2014;
            bool armorOnly = Revised(d) && Is(d, "armorer") && d.level >= 9 && slot == PlansKnown(d) - 1;
            return CharacterArtificerPlans.All.Where(p => p.edition == year && p.level <= d.level && (!armorOnly || p.armor))
                .Where(p => !p.id.StartsWith("custom.", StringComparison.Ordinal) || p.id.EndsWith(":" + slot, StringComparison.Ordinal))
                .Where(p => slot < d.artificerPlans.Count && d.artificerPlans[slot] == p.id || !d.artificerPlans.Contains(p.id))
                .Where(p => CanKnowPlans(d, d.artificerPlans.Where((_, i) => i != slot).Concat(new[] { p.id })))
                .ToArray();
        }
        internal static bool CanKnowPlans(CharacterCreationDraft d, IEnumerable<string> ids)
        {
            int year = Revised(d) ? 2024 : 2014;
            var chosen = ids.Where(id => !string.IsNullOrEmpty(id)).ToArray();
            var plans = chosen.Select(id => CharacterArtificerPlans.All.FirstOrDefault(p => p.edition == year && p.id == id)).ToArray();
            if (plans.Any(p => p == null || p.level > d.level)) return false;
            foreach (int first in new[] { 6, 10, 14 }.Where(first => first <= d.level))
            {
                var earlier = new CharacterCreationDraft { characterClass = "artificer", edition = d.edition, level = first - 1, subclass = d.subclass };
                int capacity = PlansKnown(d) - PlansKnown(earlier) + Math.Max(0, d.level - first + 1);
                if (plans.Count(p => p.level >= first) > capacity) return false;
            }
            return true;
        }
        internal static void Repair(CharacterCreationDraft d)
        {
            d.artificerPlans ??= new List<string>();
            d.artificerPlanDetails ??= new List<string>();
            d.artificerToolReplacements ??= new List<string>();
            if (!Active(d))
            {
                d.artificerPlans.Clear(); d.artificerPlanDetails.Clear(); d.artificerToolReplacements.Clear();
                d.artificerStoredSpell = d.artificerModel = "";
                CharacterArtificerEquipment.Repair(d);
                return;
            }
            int count = PlansKnown(d);
            while (d.artificerPlans.Count > count) d.artificerPlans.RemoveAt(d.artificerPlans.Count - 1);
            while (d.artificerPlans.Count < count) d.artificerPlans.Add("");
            while (d.artificerPlanDetails.Count < count) d.artificerPlanDetails.Add("");
            if (d.artificerPlanDetails.Count > count) d.artificerPlanDetails.RemoveRange(count, d.artificerPlanDetails.Count - count);
            for (int i = 0; i < count; i++)
                if (d.artificerPlans[i].Length > 0 && !PlanOptions(d, i).Any(p => p.id == d.artificerPlans[i]))
                    d.artificerPlans[i] = "";
            if (!Is(d, "armorer") || !Models(d).Contains(d.artificerModel)) d.artificerModel = "";
            if (d.level < 11) d.artificerStoredSpell = "";
            int replacements = ToolReplacementCount(d);
            while (d.artificerToolReplacements.Count > replacements) d.artificerToolReplacements.RemoveAt(d.artificerToolReplacements.Count - 1);
            while (d.artificerToolReplacements.Count < replacements) d.artificerToolReplacements.Add("");
            for (int i = 0; i < replacements; i++) if (!ToolReplacementOptions(d, i).Contains(d.artificerToolReplacements[i])) d.artificerToolReplacements[i] = "";
            var automatic = AutomaticSpells(d);
            d.preparedSpells.RemoveAll(automatic.Contains);
            d.cantrips.RemoveAll(automatic.Contains);
            CharacterArtificerEquipment.Repair(d);
        }

        public static string[] AutomaticSpells(CharacterCreationDraft d)
        {
            if (!Active(d)) return Array.Empty<string>();
            var spells = new List<string>();
            if (Revised(d)) spells.Add("Mending");
            if (d.level < 3) return spells.ToArray();
            string[] table;
            switch (d.subclass)
            {
                case "alchemist": table = new[] { "Healing Word", "Ray of Sickness", "Flaming Sphere", "Melf's Acid Arrow", "Gaseous Form", "Mass Healing Word", Revised(d) ? "Vitriolic Sphere" : "Blight", "Death Ward", "Cloudkill", "Raise Dead" }; break;
                case "armorer": table = new[] { "Magic Missile", "Thunderwave", "Mirror Image", "Shatter", "Hypnotic Pattern", "Lightning Bolt", "Fire Shield", "Greater Invisibility", "Passwall", "Wall of Force" }; break;
                case "artillerist": table = new[] { "Shield", "Thunderwave", "Scorching Ray", "Shatter", "Fireball", "Wind Wall", "Ice Storm", "Wall of Fire", "Cone of Cold", "Wall of Force" }; break;
                case "battlesmith": table = new[] { "Heroism", "Shield", Revised(d) ? "Shining Smite" : "Branding Smite", "Warding Bond", "Aura of Vitality", "Conjure Barrage", "Aura of Purity", "Fire Shield", "Banishing Smite", "Mass Cure Wounds" }; break;
                case "cartographer": table = new[] { "Faerie Fire", "Guiding Bolt", "Locate Object", "Mind Spike", "Call Lightning", "Clairvoyance", "Banishment", "Locate Creature", "Scrying", "Teleportation Circle" }; spells.Add("Healing Word"); break;
                default: return spells.ToArray();
            }
            int[] levels = { 3, 5, 9, 13, 17 };
            for (int i = 0; i < 5; i++) if (d.level >= levels[i]) spells.AddRange(table.Skip(2 * i).Take(2));
            return spells.Distinct().ToArray();
        }

        public static string[] FeatureSpells(CharacterCreationDraft d)
        {
            var result = new List<string>();
            if (Is(d, "alchemist") && d.level >= 9) result.Add("Lesser Restoration");
            if (Is(d, "alchemist") && d.level >= 15) result.AddRange(Revised(d) ? new[] { "Tasha's Bubbling Cauldron" } : new[] { "Greater Restoration", "Heal" });
            if (Is(d, "cartographer") && d.level >= 15) result.Add("Find the Path");
            if (Active(d) && d.level >= 11 && !string.IsNullOrEmpty(d.artificerStoredSpell)) result.Add(d.artificerStoredSpell);
            return result.ToArray();
        }

        internal static string ProgressionFeatures(CharacterCreationDraft d)
        {
            var lines = new List<string>();
            void Add(string id) => lines.Add(CharacterArtificerText.Get(id));
            if (d.level >= 2) lines.Add(string.Format(CharacterArtificerText.Get(Revised(d) ? "plans" : "infusions"), PlansKnown(d), ItemLimit(d)));
            if (!Revised(d) && d.level >= 3) Add("tools.job");
            if (d.level >= 6) Add(Revised(d) ? "tinker6" : "tools.expertise");
            if (d.level >= 7) Add(Revised(d) ? "genius24" : "genius14");
            if (d.level >= 10) lines.Add(string.Format(CharacterArtificerText.Get("attunement"), AttunementLimit(d)));
            if (!Revised(d) && d.level >= 10) Add("crafting14");
            if (d.level >= 11) Add(Revised(d) ? "store24" : "store14");
            if (d.level >= 14) Add(Revised(d) ? "advanced24" : "savant14");
            if (d.level >= 20) Add(Revised(d) ? "soul24" : "soul14");
            if (d.level >= 3 && Options(d).Contains(d.subclass))
            {
                Add(d.subclass + ".3." + (Revised(d) ? "24" : "14"));
                if (Revised(d)) Add(d.subclass + ".craft");
                if (Is(d, "cartographer"))
                {
                    Add("cartographer.illuminated");
                    Add("cartographer.portal");
                }
                if (Is(d, "alchemist")) Add(Revised(d) ? "elixir.table24" : "elixir.table14");
                foreach (int level in new[] { 5, 9, 15 }) if (d.level >= level) Add(d.subclass + "." + level + "." + (Revised(d) ? "24" : "14"));
                if (Is(d, "armorer") && Models(d).Contains(d.artificerModel)) Add("model." + d.artificerModel + "." + (Revised(d) ? "24" : "14"));
            }
            return lines.Count == 0 ? "" : "\n" + string.Join("\n", lines);
        }
    }
}
