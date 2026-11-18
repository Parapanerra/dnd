using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    internal static class CreationRangerCompanions
    {
        [Serializable] private sealed class Beast
        {
            public string id, name, traits, attack, damage, secondAttack, secondDamage;
            public int ac, hp, speed, attackBonus, passive, secondBonus;
            public int[] scores;
            public bool revised;
        }
        [Serializable] private sealed class Catalog { public Beast[] entries; }
        private static readonly Lazy<Beast[]> Entries = new Lazy<Beast[]>(() =>
            JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("CharacterCreation/RangerBeasts").text).entries);
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft draft, CreatedCharacterStats stats)
        {
            if (!CharacterRangerRules.Is(draft, "beastmaster")) return Array.Empty<ReadyFormTemplate>();
            return Entries.Value.Where(beast => beast.revised == CharacterRangerRules.Revised(draft))
                .Select(beast => new ReadyFormTemplate(beast.id, beast.name, (c, bindings, slot, t) => Write(c, bindings, slot, draft, stats, beast, t))).ToArray();
        }
        private static string Damage(string damage, int add)
        {
            int plus = damage.LastIndexOf('+');
            return plus >= 0 && int.TryParse(damage.Substring(plus + 1), out int flat) ? damage.Substring(0, plus) + CharacterCreationRules.Signed(flat + add)
                : int.TryParse(damage, out flat) ? (flat + add).ToString() : damage + CharacterCreationRules.Signed(add);
        }
        private static void Write(CharacterData c, CreationSheetBindings bindings, int slot, CharacterCreationDraft d, CreatedCharacterStats stats, Beast b, Func<string, string> t)
        {
            int pb = CharacterLegacySpeciesRules.Proficiency(d), wisdom = CharacterRangerRules.Wisdom(d);
            int hp = b.revised ? b.hp * (d.level + 1) : Math.Max(b.hp, 4 * d.level);
            int ac = b.ac + (b.revised ? wisdom : pb), attack = b.revised ? pb + wisdom : b.attackBonus + pb;
            string damage = Damage(b.damage, b.revised ? wisdom : pb), scene = WildShapeFormList.SceneNameForSlot(slot);
            void Text(int field, string value) => bindings.Text(c, "form.input." + field, value, scene);
            string name = t(b.name); Text(1, name); c.SetSharedString("WildShapeFormName_" + slot, name);
            Text(0, CharacterCreationRules.Signed(wisdom)); Text(44, (8 + pb + wisdom).ToString());
            Text(3, CharacterCreationRules.Signed(pb)); Text(4, (b.passive + pb).ToString()); Text(6, CharacterCreationRules.Signed(stats.initiative));
            Text(7, b.speed.ToString()); Text(8, ac.ToString());
            for (int ability = 0; ability < 6; ability++) { Text(9 + ability * 2, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(b.scores[ability]))); Text(10 + ability * 2, b.scores[ability].ToString()); }
            Text(21, CharacterCreationRules.Signed(attack)); Text(22, t(b.attack)); Text(23, damage);
            if (!string.IsNullOrEmpty(b.secondAttack)) { Text(24, CharacterCreationRules.Signed(b.secondBonus + pb)); Text(25, t(b.secondAttack)); Text(26, Damage(b.secondDamage, pb)); }
            Text(2, t(CharacterRangerText.Get("beast3" + (b.revised ? "24" : "14"))) + "\n\n" + t(b.traits));
            Text(30, string.Join("\n\n", new[] { 7, 11, 15 }.Where(level => d.level >= level).Select(level => t(CharacterRangerText.Get("beast" + level + (b.revised ? "24" : "14"))))));
            string growth = b.revised
                ? string.Format(t("ОЗ = {0} × (рівень Слідопита + 1). КО = {1} + мод. Мудрості. Атака = БМ + мод. Мудрості. Шкода = {2} + мод. Мудрості."), b.hp, b.ac, b.damage)
                : string.Format(t("ОЗ = більше з {0} та 4 × рівень Слідопита. Додавай БМ персонажа до КО {1}, атаки {2} та шкоди {3}."), b.hp, b.ac, CharacterCreationRules.Signed(b.attackBonus), b.damage);
            Text(41, growth);
            Text(42, t(CharacterRangerText.Get("beast7" + (b.revised ? "24" : "14"))) + "\n\n" + t(CharacterRangerText.Get("beast11" + (b.revised ? "24" : "14"))) + "\n\n" + t(CharacterRangerText.Get("beast15" + (b.revised ? "24" : "14"))));
            Text(43, t(b.revised ? "Змінився рівень або Мудрість — онови поля вручну. Комірка, витрачена на оживлення, не змінює показники звіра." : "Додай БМ до навичок і ряткидків, якими володіє звір. Після зміни рівня онови поля вручну."));
            var data = c.GetSceneData(scene); data.SetInt(bindings.FormHealthPrefix + "maxHealth", hp); data.SetInt(bindings.FormHealthPrefix + "currentHealth", hp);
            data.SetInt(bindings.FormHealthPrefix + "maxTemporaryHealth", 0); data.SetInt(bindings.FormHealthPrefix + "currentTemporaryHealth", 0);
        }
    }
}
