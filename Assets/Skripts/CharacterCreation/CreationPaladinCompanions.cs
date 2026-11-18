using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationPaladinCompanions
    {
        private sealed class Form
        {
            internal string id, attack, damage, secondAttack, secondDamage;
            internal int hp, ac, speed, passive = 11, hit, secondHit;
            internal int[] scores;
        }
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft draft, CreatedCharacterStats stats)
        {
            if (!CharacterPaladinRules.Active(draft) && !CharacterBardRules.Active(draft)) return Array.Empty<ReadyFormTemplate>();
            var known = draft.preparedSpells.Concat(CharacterPaladinRules.AutomaticSpells(draft)).Concat(draft.bardSecrets).ToArray();
            bool revised = CharacterPaladinRules.Revised(draft);
            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft), modifier = CharacterCreationRules.Modifier(stats.scores[5]);
            int magicHit = proficiency + modifier;
            var forms = new List<Form>();
            if (known.Contains("Find Steed"))
            {
                if (revised)
                    foreach (string type in new[] { "celestial", "fey", "fiend" })
                        forms.Add(new Form { id = "steed." + type, hp = 25, ac = 12, speed = 60,
                            scores = new[] { 18, 12, 14, 6, 12, 8 }, attack = "Потойбічний удар", damage = "1d8+2", hit = magicHit });
                else
                {
                    forms.Add(new Form { id = "warhorse", hp = 19, ac = 11, speed = 60, scores = new[] { 18, 12, 13, 6, 12, 7 }, attack = "Копита", damage = "2d6+4", hit = 6 });
                    forms.Add(new Form { id = "pony", hp = 11, ac = 10, speed = 40, scores = new[] { 15, 10, 13, 6, 11, 7 }, passive = 10, attack = "Копита", damage = "2d4+2", hit = 4 });
                    forms.Add(new Form { id = "camel", hp = 15, ac = 9, speed = 50, scores = new[] { 16, 8, 14, 6, 8, 5 }, passive = 9, attack = "Укус", damage = "1d4", hit = 5 });
                    forms.Add(new Form { id = "elk", hp = 13, ac = 10, speed = 50, scores = new[] { 16, 10, 12, 6, 10, 6 }, passive = 10, attack = "Таран", damage = "1d6+3", hit = 5, secondAttack = "Копита", secondDamage = "2d4+3", secondHit = 5 });
                    forms.Add(new Form { id = "mastiff", hp = 5, ac = 12, speed = 40, scores = new[] { 13, 14, 12, 6, 12, 7 }, passive = 13, attack = "Укус", damage = "1d6+1", hit = 3 });
                }
            }
            if (revised && known.Contains("Summon Celestial"))
                foreach (bool defender in new[] { false, true })
                    forms.Add(new Form { id = defender ? "spirit.defender" : "spirit.avenger", hp = 40, ac = defender ? 18 : 16,
                        speed = 30, passive = 12, scores = new[] { 16, 14, 16, 10, 14, 16 }, hit = magicHit,
                        attack = defender ? "Промениста булава" : "Променистий лук", damage = defender ? "1d10+8" : "2d6+7" });
            return forms.Select(form => new ReadyFormTemplate("paladin." + (revised ? "24." : "14.") + form.id,
                CharacterPaladinText.Get("form.name." + form.id),
                (character, bindings, slot, translate) => WriteOne(character, bindings, draft, form, slot, translate))).ToArray();
        }
        private static void WriteOne(CharacterData character, CreationSheetBindings bindings, CharacterCreationDraft draft,
            Form form, int slot, Func<string, string> translate)
        {
            bool revised = CharacterPaladinRules.Revised(draft);
            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft), modifier = CharacterCreationRules.Modifier(CharacterCreationRules.Scores(draft)[5]);
            string scene = WildShapeFormList.SceneNameForSlot(slot);
                void Text(int index, string value) => bindings.Text(character, "form.input." + index, value, scene);
                string name = translate(CharacterPaladinText.Get("form.name." + form.id));
                Text(1, name); character.SetSharedString("WildShapeFormName_" + slot, name);
                Text(0, CharacterCreationRules.Signed(modifier)); Text(44, (8 + proficiency + modifier).ToString());
                Text(3, CharacterCreationRules.Signed(revised ? proficiency : 2)); Text(4, form.passive.ToString());
                Text(6, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(form.scores[1])));
                Text(7, form.speed.ToString()); Text(8, form.ac.ToString());
                for (int i = 0; i < 6; i++)
                {
                    Text(9 + i * 2, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(form.scores[i])));
                    Text(10 + i * 2, form.scores[i].ToString());
                }
                Text(21, CharacterCreationRules.Signed(form.hit)); Text(22, translate(form.attack)); Text(23, form.damage);
                if (form.secondAttack != null) { Text(24, CharacterCreationRules.Signed(form.secondHit)); Text(25, translate(form.secondAttack)); Text(26, form.secondDamage); }
                Text(2, translate(CharacterPaladinText.Get("form.traits." + (form.id.StartsWith("spirit.") ? "spirit" : revised ? "steed24" : "steed14"))) + "\n\n" + translate(CharacterPaladinText.Get("form.traits." + form.id)));
                ReadyFormProgressionNotes.Paladin(character, bindings, slot, revised, form.id, form.hp, form.ac, form.damage, translate);
                var data = character.GetSceneData(scene);
                data.SetInt(bindings.FormHealthPrefix + "maxHealth", form.hp); data.SetInt(bindings.FormHealthPrefix + "currentHealth", form.hp);
                data.SetInt(bindings.FormHealthPrefix + "maxTemporaryHealth", 0); data.SetInt(bindings.FormHealthPrefix + "currentTemporaryHealth", 0);
        }
    }
}
