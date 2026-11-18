using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationStartingWeapons
    {
        internal static bool Proficient(CharacterCreationDraft d,CoreWeapon weapon,bool includeTrance=true)=>CharacterWeaponProficiency.Has(d,weapon,includeTrance);
        internal static void Write(CharacterData character,CreationSheetBindings bindings,CharacterCreationDraft draft,CreatedCharacterStats stats,Func<string,string> translate)
        {
            string Read(int index){var field=bindings.Entries.First(e=>e.key=="input."+index);var state=character.GetSceneData(field.scene);return state.GetString("StableField_v1_"+field.stableId,index<state.inputData.Count?state.inputData[index]:"");}
            var names=Enumerable.Range(0,3).Select(row=>Read(67+3*row)).ToList();
            var extra=new List<string>();
            void WriteAttack(string label, int attack, string damage)
            {
                if (string.IsNullOrWhiteSpace(label) || names.Contains(label)) return;
                int row = names.FindIndex(string.IsNullOrWhiteSpace);
                if (row < 0)
                    extra.Add(label + ": " + CharacterCreationRules.Signed(attack) + " · " + damage);
                else
                {
                    bindings.Text(character, "input." + (66 + row * 3), CharacterCreationRules.Signed(attack));
                    bindings.Text(character, "input." + (67 + row * 3), label);
                    bindings.Text(character, "input." + (68 + row * 3), damage);
                    names[row] = label;
                }
            }

            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft);
            var secondary = draft.secondaryClass?.IsSelected == true ? draft.secondaryClass.ClassDraft(draft) : null;
            if (secondary != null)
            {
                var model = CreationArtificerResources.ArmorWeapon(secondary, stats.scores, proficiency, translate);
                WriteAttack(model.name, model.bonus, model.damage);
                if (CharacterRogueRules.Is(secondary, "soulknife"))
                {
                    int modifier = CharacterCreationRules.Modifier(Math.Max(stats.scores[0], stats.scores[1]));
                    WriteAttack(translate("Психічний клинок"), proficiency + modifier, "1d6" + CharacterCreationRules.Signed(modifier));
                    WriteAttack(translate("Другий психічний клинок"), proficiency + modifier, "1d4" + CharacterCreationRules.Signed(modifier));
                }
                if (CharacterWarlockRules.Blade(secondary) && secondary.pactWeapon >= 0 && secondary.pactWeapon < CharacterWarlockRules.PactWeapons.Length)
                {
                    int modifier = CharacterWarlockRules.PactModifier(secondary, stats.scores);
                    WriteAttack(translate(CharacterWarlockRules.PactWeapons[secondary.pactWeapon]), proficiency + modifier,
                        CharacterWarlockRules.PactDamage[secondary.pactWeapon] + CharacterCreationRules.Signed(modifier));
                }
            }
            // The Artificer writer includes both class and background weapons, with their individual infusions.
            var weapons = CharacterArtificerRules.Active(draft) ? Array.Empty<string>() :
                CharacterCreationRules.StartingWeapons(draft).Concat(CharacterBackgroundRules.Weapons(draft));
            var artificer = secondary?.characterClass == "artificer" ? secondary : null;
            foreach(string name in weapons.Where(w=>!string.IsNullOrWhiteSpace(w)).Distinct())
            {
                string label=translate(name);
                int enhancement = 0;
                if (artificer != null)
                {
                    string target = CharacterArtificerEquipment.Weapons(artificer).FirstOrDefault(choice => choice.name == name).target;
                    if (target != null) enhancement = CharacterArtificerEquipment.Bonus(artificer, target);
                }
                if (names.Contains(label))
                {
                    if (enhancement > 0)
                        for (int existing = 0; existing < names.Count; existing++)
                        {
                            if (names[existing] != label) continue;
                            int attackIndex = 66 + 3 * existing;
                            if (int.TryParse(Read(attackIndex), out int previous))
                                bindings.Text(character, "input." + attackIndex, CharacterCreationRules.Signed(previous + enhancement));
                            string original = Read(attackIndex + 2);
                            var match = System.Text.RegularExpressions.Regex.Match(original, @"^\d+d\d+([+-]\d+)?");
                            if (match.Success)
                            {
                                string originalModifier = match.Groups[1].Value;
                                int bonus = (int.TryParse(originalModifier, out int value) ? value : 0) + enhancement;
                                string updatedDamage = match.Value.Substring(0, match.Length - originalModifier.Length) +
                                    CharacterCreationRules.Signed(bonus) + original.Substring(match.Length);
                                bindings.Text(character, "input." + (attackIndex + 2), updatedDamage);
                            }
                        }
                    continue;
                }
                var weapon=CharacterWeaponCatalog.Find(name,draft.edition);if(weapon==null)throw new InvalidOperationException("Unknown starting weapon: "+name);
                int modifier=draft.characterClass=="monk"?CharacterMonkRules.WeaponModifier(draft,stats.scores,name):CharacterBloodHunterRules.WeaponModifier(name,stats.scores);
                int attack=modifier+enhancement+(Proficient(draft,weapon)?CharacterLegacySpeciesRules.Proficiency(draft):0);
                bool style=draft.characterClass=="fighter"||draft.level>=2&&new[]{"ranger","paladin","bloodhunter"}.Contains(draft.characterClass);
                bool HasStyle(FighterStyle choice) => style && CharacterFighterRules.HasStyle(draft, choice) ||
                    secondary != null && CharacterFighterRules.HasStyle(secondary, choice);
                if(weapon.ranged&&HasStyle(FighterStyle.Archery))attack+=2;
                int damageBonus=modifier+enhancement+(!weapon.ranged&&!weapon.twoHanded&&HasStyle(FighterStyle.Dueling)?2:0);
                string dice=draft.characterClass=="monk"?CharacterMonkRules.WeaponDamage(draft,name):weapon.damage;
                string damage=dice.Length>0?dice+CharacterCreationRules.Signed(damageBonus):"";
                WriteAttack(label, attack, damage);
            }
            if(extra.Count>0)
            {
                string previous=Read(75);string text=(previous.Length==0?"":previous+"\n\n")+string.Join("\n",extra);
                if(text.Length>2800)throw new InvalidOperationException("Starting attacks exceed the attack description field.");
                bindings.Text(character,"input.75",text);
            }
        }
    }
}
