using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationArtificerCompanions
    {
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            if (!CharacterArtificerRules.Active(d)) return Array.Empty<ReadyFormTemplate>();
            var result = new List<ReadyFormTemplate>();
            bool modern = CharacterArtificerRules.Revised(d);
            void Add(string kind, string title) => result.Add(new ReadyFormTemplate("artificer." + (modern ? "24." : "14.") + kind,
                CharacterArtificerText.Get(title), (c, b, slot, t) => Write(c, b, slot, d, kind, title, t)));
            if (CharacterArtificerRules.Is(d, "battlesmith")) Add("defender", "form.defender");
            if (modern ? d.preparedSpells.Contains("Homunculus Servant") : d.artificerPlans.Contains("Homunculus Servant")) Add("homunculus", "form.homunculus");
            if (CharacterArtificerRules.Is(d, "artillerist"))
            {
                if (modern) Add("cannon", "form.cannon");
                else for (int i = 0; i < 3; i++) Add("cannon" + i, "form.cannon." + i);
            }
            if (!modern && d.preparedSpells.Contains("Summon Construct")) for (int i = 0; i < 3; i++)
            {
                int variant = i;
                result.Add(new ReadyFormTemplate("artificer.14.construct." + i, CharacterArtificerText.Get("form.construct." + i),
                    (c, b, slot, t) => Construct(c, b, slot, d, variant, t)));
            }
            return result.ToArray();
        }

        private static void Construct(CharacterData c, CreationSheetBindings b, int slot, CharacterCreationDraft d, int variant, Func<string, string> t)
        {
            string scene = WildShapeFormList.SceneNameForSlot(slot);
            void Put(int index, string value) => b.Text(c, "form.input." + index, value, scene);
            string Text(string id) => t(CharacterArtificerText.Get(id));
            int pb = CharacterLegacySpeciesRules.Proficiency(d), modifier = CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[3]);
            string name = Text("form.construct." + variant); Put(1, name); c.SetSharedString("WildShapeFormName_" + slot, name);
            Put(0, CharacterCreationRules.Signed(modifier)); Put(3, CharacterCreationRules.Signed(pb)); Put(4, "10"); Put(6, "+0"); Put(7, "30"); Put(8, "17"); Put(44, (8 + pb + modifier).ToString());
            int[] scores = { 18, 10, 18, 14, 11, 5 };
            for (int i = 0; i < 6; i++) { Put(9 + 2 * i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i]))); Put(10 + 2 * i, scores[i].ToString()); }
            Put(21, CharacterCreationRules.Signed(pb + modifier)); Put(22, Text("form.slam")); Put(23, "1d8+8");
            Put(2, Text("form.construct.common") + "\n\n" + Text("form.construct.trait." + variant));
            Put(30, Text("form.construct.attacks"));
            Put(41, Text("form.table.slot") + "\n4: 40 · 17 · 1d8+8\n5: 55 · 18 · 1d8+9"); Put(42, Text("form.progression")); Put(43, Text("form.intelligence"));
            var state = c.GetSceneData(scene);
            state.SetInt(b.FormHealthPrefix + "maxHealth", 40); state.SetInt(b.FormHealthPrefix + "currentHealth", 40);
            state.SetInt(b.FormHealthPrefix + "maxTemporaryHealth", 0); state.SetInt(b.FormHealthPrefix + "currentTemporaryHealth", 0);
        }

        private static void Write(CharacterData c, CreationSheetBindings b, int slot, CharacterCreationDraft d, string kind, string title, Func<string, string> t)
        {
            bool modern = CharacterArtificerRules.Revised(d), defender = kind == "defender", homunculus = kind == "homunculus";
            int level = d.level, pb = CharacterLegacySpeciesRules.Proficiency(d), intellect = CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[3]);
            int hp = defender ? modern ? 5 + 5 * level : 2 + intellect + 5 * level : homunculus ? modern ? 15 : 1 + intellect + level : 5 * level;
            int ac = defender ? modern ? 12 + intellect : 15 + (level >= 15 ? 2 : 0) : homunculus ? 13 : 18;
            string scene = WildShapeFormList.SceneNameForSlot(slot);
            string Text(string key) => t(CharacterArtificerText.Get(key));
            void Put(int field, string value) => b.Text(c, "form.input." + field, value, scene);
            string name = Text(title); Put(1, name); c.SetSharedString("WildShapeFormName_" + slot, name);
            Put(0, CharacterCreationRules.Signed(intellect)); Put(3, CharacterCreationRules.Signed(pb));
            Put(4, (defender && !modern ? 10 + pb * 2 : homunculus && !modern ? 10 + pb * 2 : 10).ToString());
            Put(6, defender ? "+1" : homunculus ? "+2" : "+0"); Put(7, defender ? "40" : homunculus ? "20" : "15"); Put(8, ac.ToString());
            Put(44, (8 + pb + intellect).ToString());
            int[] scores = defender ? new[] { 14, 12, 14, 4, 10, 6 } : homunculus ? new[] { 4, 15, 12, 10, 10, 7 } : new[] { 10, 10, 10, 10, 10, 10 };
            for (int i = 0; i < 6; i++)
            {
                Put(9 + 2 * i, !defender && !homunculus && modern ? "" : CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i])));
                Put(10 + 2 * i, !defender && !homunculus && modern ? "" : scores[i].ToString());
            }
            Put(2, Text(defender ? modern ? "form.defender24" : "form.defender14" : homunculus ? modern ? "form.homunculus24" : "form.homunculus14" : modern ? "form.cannon24" : "form.cannon14"));
            string attack = CharacterCreationRules.Signed(pb + intellect);
            int die = level >= 9 ? 3 : 2;
            if (defender || homunculus)
            {
                Put(21, attack); Put(22, Text(defender ? "form.rend" : "form.force"));
                Put(23, defender ? "1d8" + CharacterCreationRules.Signed(modern ? 2 + intellect : pb) : modern ? "1d6+2" : "1d4+" + pb);
                Put(30, Text(defender ? "form.defender.actions" : "form.homunculus.actions"));
                if (defender && level >= 15) Put(40, Text("form.deflect") + ": 1d4" + CharacterCreationRules.Signed(intellect));
            }
            else
            {
                if (modern || kind == "cannon1") { Put(21, attack); Put(22, Text("form.ballista")); Put(23, die + "d8"); }
                if (modern || kind == "cannon0") { Put(modern ? 25 : 22, Text("form.flamethrower")); Put(modern ? 26 : 23, die + "d8"); }
                if (modern || kind == "cannon2") { Put(modern ? 28 : 22, Text("form.protector")); Put(modern ? 29 : 23, (modern && level >= 9 ? "2d8" : "1d8") + CharacterCreationRules.Signed(Math.Max(1, intellect))); }
                Put(30, Text("form.cannon.actions"));
            }
            var table = new List<string> { Text(homunculus && modern ? "form.table.slot" : "form.table.level") };
            if (homunculus && modern)
                for (int circle = 2; circle <= 5; circle++) table.Add(circle + ": " + (5 + 5 * circle) + " · 13 · 1d6+" + circle);
            else if (!defender && !homunculus)
            {
                table.Clear();table.Add(Text("form.cannon.compact"));
            }
            else for (int n = defender ? 3 : homunculus ? 2 : 3; n <= 20; n++)
            {
                int bonus = 2 + (n - 1) / 4;
                int nextHP = defender ? modern ? 5 + 5 * n : 2 + intellect + 5 * n : homunculus ? 1 + intellect + n : 5 * n;
                int nextAC = defender ? modern ? 12 + intellect : 15 + (n >= 15 ? 2 : 0) : homunculus ? 13 : 18;
                string damage = defender ? "1d8" + CharacterCreationRules.Signed(modern ? 2 + intellect : bonus) : homunculus ? "1d4+" + bonus : (n >= 9 ? "3d8" : "2d8");
                table.Add(n + ": " + nextHP + " · " + nextAC + " · " + damage);
            }
            Put(41, string.Join("\n", table)); Put(42, Text("form.progression"));
            Put(43, Text(defender ? "form.defender.growth" : homunculus ? "form.homunculus.growth" : "form.cannon.growth"));
            var state = c.GetSceneData(scene);
            state.SetInt(b.FormHealthPrefix + "maxHealth", Math.Max(1, hp)); state.SetInt(b.FormHealthPrefix + "currentHealth", Math.Max(1, hp));
            state.SetInt(b.FormHealthPrefix + "maxTemporaryHealth", 0); state.SetInt(b.FormHealthPrefix + "currentTemporaryHealth", 0);
        }
    }
}
