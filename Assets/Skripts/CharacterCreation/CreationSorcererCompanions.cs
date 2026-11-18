using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationSorcererCompanions
    {
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            if (!CharacterSorcererRules.Active(d) && !CharacterBardRules.Active(d) && !CharacterArtificerRules.Active(d)) return Array.Empty<ReadyFormTemplate>();
            var spells = d.preparedSpells.Concat(CharacterSorcererRules.AutomaticSpells(d)).Concat(d.bardSecrets).ToArray();
            var result = new List<ReadyFormTemplate>();
            if (spells.Contains("Summon Dragon") && CharacterSorcererRules.Revised(d))
                for (int i = 0; i < 5; i++) {
                    int element = i;
                    result.Add(new ReadyFormTemplate("summon.dragon." + i, CharacterSorcererText.Get("form.dragon." + i),
                        (c, b, slot, t) => Dragon(c, b, slot, d, element, t)));
                }
            if (spells.Contains("Animate Objects"))
                for (int i = 0; i < (CharacterSorcererRules.Revised(d) ? 3 : 5); i++) {
                    int size = i;
                    result.Add(new ReadyFormTemplate("animate." + (CharacterSorcererRules.Revised(d) ? "24." : "14.") + i,
                        CharacterSorcererText.Get("form.object." + (CharacterSorcererRules.Revised(d) ? "24." : "14.") + i),
                        (c, b, slot, t) => Animated(c, b, slot, d, size, t)));
                }
            if (spells.Contains("Bigby's Hand") && CharacterSorcererRules.Revised(d))
                result.Add(new ReadyFormTemplate("spell.bigby.hand", "Рука Біґбі", (c, b, slot, t) => Hand(c, b, slot, d, stats, t)));
            return result.ToArray();
        }
        private sealed class Form
        {
            private readonly CharacterData c;
            private readonly CreationSheetBindings b;
            private readonly string scene;
            internal Form(CharacterData character, CreationSheetBindings bindings, int slot) { c = character; b = bindings; scene = WildShapeFormList.SceneNameForSlot(slot); }
            internal void Text(int n, string value) => b.Text(c, "form.input." + n, value, scene);
            internal void Health(int hp) {
                var s = c.GetSceneData(scene); s.SetInt(b.FormHealthPrefix + "maxHealth", hp); s.SetInt(b.FormHealthPrefix + "currentHealth", hp);
                s.SetInt(b.FormHealthPrefix + "maxTemporaryHealth", 0); s.SetInt(b.FormHealthPrefix + "currentTemporaryHealth", 0);
            }
            internal void Scores(int[] scores) { for (int i = 0; i < 6; i++) { Text(9 + i * 2, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i]))); Text(10 + i * 2, scores[i].ToString()); } }
        }
        private static int Modifier(CharacterCreationDraft d) => CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[CharacterArtificerRules.Active(d) ? 3 : 5]);
        private static int Bonus(CharacterCreationDraft d) => CharacterLegacySpeciesRules.Proficiency(d);
        private static void Header(Form f, CharacterData c, int slot, string name, int ac, int speed, int initiative)
        { f.Text(1, name); c.SetSharedString("WildShapeFormName_" + slot, name); f.Text(8, ac.ToString()); f.Text(7, speed.ToString()); f.Text(6, CharacterCreationRules.Signed(initiative)); }
        private static void Dragon(CharacterData c, CreationSheetBindings b, int slot, CharacterCreationDraft d, int element, Func<string,string> t)
        {
            var f = new Form(c, b, slot); int modifier = Modifier(d), pb = Bonus(d), dc = 8 + pb + modifier;
            Header(f, c, slot, t(CharacterSorcererText.Get("form.dragon." + element)), 19, 30, 2); f.Health(50); f.Scores(new[] { 19,14,17,10,14,14 });
            f.Text(0, CharacterCreationRules.Signed(modifier)); f.Text(3, CharacterCreationRules.Signed(pb)); f.Text(44, dc.ToString()); f.Text(4, "12");
            f.Text(21, CharacterCreationRules.Signed(pb + modifier)); f.Text(22, t("Роздирання")); f.Text(23, "1d6+9");
            f.Text(24, dc.ToString()); f.Text(25, t("Дихання дракона")); f.Text(26, "2d6");
            f.Text(2, t(CharacterSorcererText.Get("form.dragon.traits")));
            f.Text(30, t(CharacterSorcererText.Get("form.dragon.breath")) + "\n" + t("Тип шкоди:") + " " + t(CharacterSorcererRules.Elements[element]) + ".");
            f.Text(41, ReadyFormProgressionNotes.SlotGrowth(5, 50, 19, 10, "1d6+9", t));
            f.Text(42, t(CharacterSorcererText.Get("form.dragon.growth")));
            f.Text(43, t(CharacterArtificerRules.Active(d) ? CharacterArtificerText.Get("form.intelligence") : CharacterSorcererText.Get("form.charisma")));
        }
        private static void Animated(CharacterData c, CreationSheetBindings b, int slot, CharacterCreationDraft d, int size, Func<string,string> t)
        {
            bool revised = CharacterSorcererRules.Revised(d); int modifier = Modifier(d), pb = Bonus(d);
            var f = new Form(c, b, slot);
            int hp = revised ? new[] {10,20,40}[size] : new[] {20,25,40,50,80}[size];
            int ac = revised ? 15 : new[] {18,16,13,10,10}[size];
            int attack = revised ? pb + modifier : new[] {8,6,5,6,8}[size];
            int str = revised ? 16 : new[] {4,6,10,14,18}[size], dex = revised ? 10 : new[] {18,14,12,10,6}[size];
            Header(f, c, slot, t(CharacterSorcererText.Get("form.object." + (revised ? "24." : "14.") + size)), ac, 30, CharacterCreationRules.Modifier(dex));
            f.Health(hp); f.Scores(new[] {str,dex,10,3,3,1}); f.Text(4, "6");
            f.Text(3, revised ? CharacterCreationRules.Signed(pb) : ""); f.Text(21, CharacterCreationRules.Signed(attack)); f.Text(22, t("Удар предмета"));
            string damage = revised ? (size == 0 ? "1d4" : size == 1 ? "2d6" : "2d12") + CharacterCreationRules.Signed(3 + modifier) : new[] {"1d4+4","1d8+2","2d6+1","2d10+2","2d12+4"}[size];
            f.Text(23, damage); f.Text(2, t(CharacterSorcererText.Get(revised ? "form.object.traits24" : "form.object.traits14")));
            f.Text(30, t(CharacterSorcererText.Get(revised ? "form.object.damage24" : "form.object.damage14")));
            f.Text(41, string.Format(t("Комірка {0}: {1} ОЗ · КО {2} · шкода {3}."), 5, hp, ac, damage));
            f.Text(42, t(CharacterSorcererText.Get(revised ? "form.object.growth24" : "form.object.growth14")));
            f.Text(43, revised ? t(CharacterArtificerRules.Active(d) ? CharacterArtificerText.Get("form.intelligence") : CharacterSorcererText.Get("form.charisma")) : t(CharacterSorcererText.Get("form.object.manual14")));
        }
        private static void Hand(CharacterData c, CreationSheetBindings b, int slot, CharacterCreationDraft d, CreatedCharacterStats stats, Func<string,string> t)
        {
            var f = new Form(c, b, slot); int modifier = Modifier(d), pb = Bonus(d), dc = 8 + pb + modifier;
            Header(f, c, slot, t("Рука Біґбі"), 20, 60, 0); f.Health(stats.hitPoints);
            f.Text(9, "+8"); f.Text(10, "26"); f.Text(11, "+0"); f.Text(12, "10");
            f.Text(21, CharacterCreationRules.Signed(pb + modifier)); f.Text(22, t("Стиснутий кулак")); f.Text(23, "5d8");
            f.Text(24, dc.ToString()); f.Text(25, t("Схоплення й стискання")); f.Text(26, "4d6" + CharacterCreationRules.Signed(modifier));
            f.Text(27, dc.ToString()); f.Text(28, t("Поштовх руки")); f.Text(29, (5 + 5 * modifier).ToString());
            f.Text(2, t(CharacterSorcererText.Get("form.hand.traits")));
            f.Text(30, t(CharacterSorcererText.Get("form.hand.actions")));
            f.Text(41, string.Format(t("Комірка 5: кулак {0}, стискання {1}."), "5d8", "4d6" + CharacterCreationRules.Signed(modifier)));
            f.Text(42, t(CharacterSorcererText.Get("form.hand.growth")));
            f.Text(43, t(CharacterArtificerRules.Active(d) ? CharacterArtificerText.Get("form.intelligence") : CharacterSorcererText.Get("form.charisma")));
        }
    }
}
