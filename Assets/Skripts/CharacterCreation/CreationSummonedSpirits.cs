using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    internal static class CreationSummonedSpirits
    {
        [Serializable] private sealed class Spirit
        {
            public string id, spell, name, attack, damage, damageType, traits;
            public int level, hp, ac, speed, range, hpPerLevel;
            public int[] scores;
        }
        [Serializable] private sealed class Catalog { public Spirit[] entries; }
        private static readonly Lazy<Spirit[]> Entries = new Lazy<Spirit[]>(Load);

        private static Spirit[] Load()
        {
            var asset = Resources.Load<TextAsset>("CharacterCreation/SummonedSpirits");
            if (asset == null) throw new InvalidOperationException("Summoned spirit records are missing.");
            return JsonUtility.FromJson<Catalog>(asset.text).entries;
        }

        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft draft, CreatedCharacterStats stats)
        {
            if (!(CharacterWizardRules.Active(draft) || CharacterWarlockRules.Active(draft) || CharacterDruidRules.Active(draft) || CharacterRogueRules.Trickster(draft) || CharacterFighterRules.Caster(draft) || CharacterRangerRules.Active(draft) || CharacterSorcererRules.Active(draft) || CharacterBardRules.Active(draft) || CharacterArtificerRules.Active(draft)) || draft.edition != RulesEdition.Rules2024) return Array.Empty<ReadyFormTemplate>();
            var spells = draft.preparedSpells.Concat(CharacterDruidRules.AutomaticSpells(draft))
                .Concat(CharacterRangerRules.AutomaticSpells(draft)).Concat(CharacterSorcererRules.AutomaticSpells(draft))
                .Concat(draft.bardSecrets).Concat(CharacterWarlockRules.AutomaticSpells(draft))
                .Concat(draft.warlockArcana).Concat(CharacterWizardRules.Automatic(draft))
                .ToHashSet(StringComparer.Ordinal);
            return Entries.Value.Where(spirit => spells.Contains(spirit.spell))
                .Select(spirit => new ReadyFormTemplate(spirit.id, spirit.name,
                    (character, bindings, slot, translate) => WriteOne(character, bindings, draft, stats, spirit, slot, translate))).ToArray();
        }
        private static void WriteOne(CharacterData character, CreationSheetBindings bindings, CharacterCreationDraft draft, CreatedCharacterStats stats,
            Spirit spirit, int slot, Func<string, string> translate)
        {
            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft);
            int castingAbility = CreationSpellPreparation.ClassAbility(draft);
            bool wisdom = castingAbility == 4, charisma = castingAbility == 5;
            int modifier = CharacterCreationRules.Modifier(stats.scores[castingAbility]);
            string scene = WildShapeFormList.SceneNameForSlot(slot);
                void Text(int index, string text) => bindings.Text(character, "form.input." + index, text, scene);
                string name = translate(spirit.name); Text(1, name); character.SetSharedString("WildShapeFormName_" + slot, name);
                Text(0, CharacterCreationRules.Signed(modifier)); Text(44, (8 + proficiency + modifier).ToString());
                Text(3, CharacterCreationRules.Signed(proficiency)); Text(4, spirit.spell == "Summon Beast" ? "12" : "10"); Text(6, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(spirit.scores[1])));
                Text(7, spirit.speed.ToString()); Text(8, spirit.ac.ToString());
                for (int ability = 0; ability < 6; ability++) { Text(9 + 2 * ability, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(spirit.scores[ability]))); Text(10 + 2 * ability, spirit.scores[ability].ToString()); }
                Text(21, CharacterCreationRules.Signed(proficiency + modifier)); Text(22, translate(spirit.attack)); Text(23, spirit.damage);
                Text(2, string.Format(translate(spirit.spell == "Summon Beast" ? "Комірка {0}: атак за дію — {1}. Діє одразу після тебе; усні накази без дії. Зникає при 0 ОЗ або завершенні спела. Розуміє твої мови." : "Комірка {0}: атак за дію — {1}. Діє одразу після тебе; усні накази без дії. Зникає при 0 ОЗ або завершенні спела. Темнозір 60 футів; розуміє твої мови."), spirit.level, spirit.level / 2) + "\n\n" + translate(spirit.traits));
                Text(30, string.Format(translate(charisma ? "Шкода: {0}. Дальність / досяжність: {1} футів. Атака й СК використовують твою магію Харизми." : wisdom ? "Шкода: {0}. Дальність / досяжність: {1} футів. Атака й СК використовують твою магію Мудрості." : "Шкода: {0}. Дальність / досяжність: {1} футів. Атака й СК використовують твою магію Інтелекту."), translate(spirit.damageType), spirit.range));
                ReadyFormProgressionNotes.Spirit(character, bindings, slot, spirit.level, spirit.hp, spirit.ac, spirit.hpPerLevel, spirit.damage, translate, wisdom, charisma);
                var data = character.GetSceneData(scene);
                data.SetInt(bindings.FormHealthPrefix + "maxHealth", spirit.hp); data.SetInt(bindings.FormHealthPrefix + "currentHealth", spirit.hp);
                data.SetInt(bindings.FormHealthPrefix + "maxTemporaryHealth", 0); data.SetInt(bindings.FormHealthPrefix + "currentTemporaryHealth", 0);
        }
    }
}
