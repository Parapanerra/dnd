using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationArtificerResources
    {
        private const string AttackVersionKey = "CreationArtificerAttacks_v1";

        internal static void ApplyEquipmentChange(CharacterData character, CharacterCreationDraft previous,
            CharacterCreationDraft next, CreationSheetBindings bindings, Func<string, string> translate)
        {
            var history = CharacterLevelUp.ReadClassHistory(character, previous);
            if (history == null || !history.MatchesDraft(next)) throw new InvalidOperationException("Невідповідність історії рівнів персонажа.");
            var builder = new CharacterCreationService(bindings, translate);
            string[] order = character.GetSharedString(CharacterLevelUp.SpellOrderKey).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var before = builder.Build(previous, order, history);
            var after = builder.Build(next, order, history);
            var candidate = UnityEngine.JsonUtility.FromJson<CharacterData>(UnityEngine.JsonUtility.ToJson(character));
            GeneratedCharacterMerge.Apply(candidate, before, after, bindings, translate);
            foreach (var state in candidate.sceneStates)
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(state), character.GetSceneData(state.sceneName));
            character.maxHealth = candidate.maxHealth;
            character.currentHealth = candidate.currentHealth;
            character.sharedStringData = candidate.sharedStringData;
            character.SetSharedString(CharacterLevelUp.DraftKey, after.GetSharedString(CharacterLevelUp.DraftKey));
        }

        internal static void Write(CharacterData c, CreationSheetBindings b, CharacterCreationDraft d, Func<string, string> t)
        {
            if (!CharacterArtificerRules.Active(d)) return;
            bool modern = CharacterArtificerRules.Revised(d);
            int intellect = Math.Max(1, CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[3]));
            string Label(string key) => t(CharacterArtificerText.Get(key));
            string Add(string key, int uses) => CreationMagicResources.Add(c, b, Label(key), uses, false, useLastRow: d.IsSecondaryTrack);
            b.Dropdown(c, "magic.infusion.count", d.artificerPlans.Count);
            for (int i = 0; i < 12; i++)
            {
                var plan = i < d.artificerPlans.Count ? CharacterArtificerPlans.All.First(p => p.edition == (modern ? 2024 : 2014) && p.id == d.artificerPlans[i]) : null;
                b.Text(c, "magic.infusion." + i + ".name", plan == null ? "" : plan.id.StartsWith("custom.", StringComparison.Ordinal) ? d.artificerPlanDetails[i].Trim() : t(plan.name));
                // Known choices are not automatically infused into an unspecified object.
                b.Toggle(c, "magic.infusion." + i + ".prepared", plan != null && CharacterArtificerEquipment.Target(d, i).Length > 0);
            }
            if (d.level >= 7)
            {
                string key = Add("pool.genius", intellect);
                if (modern && d.level >= 14) CreationResourceRecovery.RegisterPartial(c, b, key, 1, false);
            }
            if (modern && d.level >= 6) { Add("pool.drain", 1); Add("pool.transmute", 1); }
            if (d.level >= 11)
            {
                int uses = 2 * intellect;
                Add("pool.store", Math.Min(8, uses));
                if (uses > 8) Add("pool.store.extra", uses - 8);
            }
            if (CharacterArtificerRules.Is(d, "alchemist"))
            {
                Add("pool.elixirs", CharacterArtificerRules.Elixirs(d));
                if (d.level >= 9) Add("pool.restoration", intellect);
                if (d.level >= 15)
                {
                    if (modern) Add("pool.cauldron", 1);
                    else { Add("pool.greater", 1); Add("pool.heal", 1); }
                }
            }
            if (CharacterArtificerRules.Is(d, "artillerist")) Add("pool.cannon", 1);
            if (CharacterArtificerRules.Is(d, "battlesmith") && d.level >= 9) Add("pool.jolt", intellect);
            if (CharacterArtificerRules.Is(d, "cartographer"))
            {
                Add("pool.faerie", intellect);
                if (d.level >= 15) Add("pool.path", 1);
            }
            if (CharacterArtificerRules.Is(d, "armorer") && CharacterArtificerEquipment.WearsArmor(d))
            {
                if (d.artificerModel == "dreadnaught") Add("pool.giant", intellect);
                if (d.artificerModel == "guardian" && !modern) Add("pool.field", CharacterLegacySpeciesRules.Proficiency(d));
                if (d.level >= 15 && d.artificerModel == "guardian") Add("pool.pull", modern ? intellect : CharacterLegacySpeciesRules.Proficiency(d));
                if (d.level >= 15 && modern && d.artificerModel == "infiltrator") Add("pool.flight", intellect);
            }
            if (!d.IsSecondaryTrack)
            {
                WriteWeapons(c, b, d, t);
                c.SetSharedString(AttackVersionKey, "1");
            }
        }

        private static void WriteWeapons(CharacterData c, CreationSheetBindings b, CharacterCreationDraft d, Func<string, string> t)
        {
            var attacks = StartingAttacks(d, t);
            for (int i = 0; i < attacks.rows.Length; i++) b.Text(c, "input." + (66 + i), attacks.rows[i]);
            if (attacks.text.Length > 0) b.Text(c, "input.75", attacks.text);
        }

        private static CreationAttackOverflow.Baseline StartingAttacks(CharacterCreationDraft d, Func<string, string> t)
        {
            bool modern = CharacterArtificerRules.Revised(d);
            var attacks = new System.Collections.Generic.List<(string name, int bonus, string damage)>();
            int[] scores = CharacterCreationRules.Scores(d);
            int proficiency = CharacterLegacySpeciesRules.Proficiency(d);
            var weapons = CharacterArtificerEquipment.Weapons(d).ToArray();
            int beforeArmor = modern ? 1 : 2;

            void AddWeapon((string target, string name) choice)
            {
                if (string.IsNullOrWhiteSpace(choice.name)) return;
                var weapon = CharacterWeaponCatalog.Find(choice.name, d.edition);
                int enhancement = CharacterArtificerEquipment.Bonus(d, choice.target);
                int ability = weapon.ranged ? scores[1] : weapon.finesse ? Math.Max(scores[0], scores[1]) : scores[0];
                if (enhancement > 0 && CharacterArtificerRules.Is(d, "battlesmith")) ability = Math.Max(ability, scores[3]);
                int modifier = CharacterCreationRules.Modifier(ability) + enhancement;
                string name = t(choice.name) + (enhancement > 0 ? " +" + enhancement : "");
                var attack = (name, proficiency + modifier, weapon.damage + CharacterCreationRules.Signed(modifier));
                if (!attacks.Contains(attack)) attacks.Add(attack);
            }

            foreach (var weapon in weapons.Take(beforeArmor)) AddWeapon(weapon);
            var armorWeapon = ArmorWeapon(d, scores, proficiency, t);
            if (armorWeapon.name != null) attacks.Add(armorWeapon);
            foreach (var weapon in weapons.Skip(beforeArmor)) AddWeapon(weapon);
            var rows = Enumerable.Repeat("", 9).ToArray();
            for (int row = 0; row < Math.Min(3, attacks.Count); row++)
            {
                rows[3 * row] = CharacterCreationRules.Signed(attacks[row].bonus);
                rows[3 * row + 1] = attacks[row].name;
                rows[3 * row + 2] = attacks[row].damage;
            }
            return new CreationAttackOverflow.Baseline { rows = rows, text = string.Join("\n", attacks.Skip(3).Select(attack =>
                attack.name + ": " + CharacterCreationRules.Signed(attack.bonus) + " · " + attack.damage)) };
        }

        internal static (string name, int bonus, string damage) ArmorWeapon(CharacterCreationDraft d, int[] scores,
            int proficiency, Func<string, string> translate)
        {
            if (!CharacterArtificerRules.Is(d, "armorer") || !CharacterArtificerEquipment.WearsArmor(d)) return default;
            bool modern = CharacterArtificerRules.Revised(d);
            int infused = CharacterArtificerEquipment.Bonus(d, "armor.weapon");
            int modifier = CharacterCreationRules.Modifier(scores[3]) + infused + (modern && d.level >= 9 ? 1 : 0);
            string dice = d.artificerModel == "guardian" ? modern && d.level >= 15 ? "1d10" : "1d8" :
                d.artificerModel == "dreadnaught" ? d.level >= 15 ? "2d6" : "1d10" : modern && d.level >= 15 ? "2d6" : "1d6";
            string name = translate(CharacterArtificerText.Get(d.artificerModel == "guardian" && !modern
                ? "weapon.guardian14" : "weapon." + d.artificerModel));
            if (infused > 0) name += " +" + infused;
            return (name, proficiency + modifier, dice + CharacterCreationRules.Signed(modifier));
        }

        internal static string[] InfusionNoteTexts(CharacterCreationDraft d, Func<string,string> t)
            => CreationDescriptionLayout.Pack(InfusionDescriptions(d, t), 4);

        internal static System.Collections.Generic.IEnumerable<string> InfusionDescriptions(CharacterCreationDraft d, Func<string,string> t)
        {
            // These are learned plans, not automatically equipped items. Keep explanations
            // beside the native preparation list rather than duplicating them in sheet notes.
            bool modern=CharacterArtificerRules.Revised(d);
            return d.artificerPlans.Select((id,index)=>
            {
                var plan=CharacterArtificerPlans.All.FirstOrDefault(p=>p.edition==(modern?2024:2014)&&p.id==id);
                if(plan==null)return "";
                bool custom=id.StartsWith("custom.",StringComparison.Ordinal);
                string name=custom?index<d.artificerPlanDetails.Count?d.artificerPlanDetails[index].Trim():"":t(plan.name);
                string effect=custom?CharacterDescriptionText.Get("item.custom"):!modern?CharacterArtificerInfusionEffects.Get(id):"";
                if(effect.Length==0)effect=CharacterArtificerItemEffects.Get(id,modern);
                if(effect.Length==0)throw new InvalidOperationException("Missing Artificer plan description: "+id);
                return name+":\n"+t(effect);
            }).Where(p=>p.Length>0);
        }

        // Original base-infusion notes are recognized only by exact text equality.
        internal static string[] LegacyInfusionNoteTexts(CharacterCreationDraft d,Func<string,string> t)
        {
            var notes=new string[]{"","","",""};
            if(CharacterArtificerRules.Revised(d))return notes;
            var paragraphs=d.artificerPlans.Select(id=>new{id,effect=CharacterArtificerInfusionEffects.Get(id)})
                .Where(p=>p.effect.Length>0).Select(p=>t(p.id)+":\n"+t(p.effect));
            int row=0; string note="";
            foreach(string paragraph in paragraphs)
            {
                string next=note.Length==0?paragraph:note+"\n\n"+paragraph;
                if(next.Length>2800&&note.Length>0)
                {
                    if(row>=notes.Length)throw new InvalidOperationException("Infusion notes exceed four panels.");
                    notes[row]=note;
                    row++;note=paragraph;
                }
                else note=next;
            }
            if(note.Length>0){if(row>=notes.Length)throw new InvalidOperationException("Infusion notes exceed four panels.");notes[row]=note;}
            return notes;
        }


        internal static bool ApplyEquipmentToggle(CharacterData character, CreationSheetBindings bindings,
            CharacterCreationDraft draft, int slot, bool equipped, Func<string, string> translate, string chosenTarget = null)
        {
            if (slot < 0 || slot >= draft.artificerPlans.Count) return false;
            var before = UnityEngine.JsonUtility.FromJson<CharacterCreationDraft>(UnityEngine.JsonUtility.ToJson(draft));
            string targetKey = "ArtificerTarget_" + slot;
            string target = CharacterArtificerEquipment.Target(draft, slot);
            if (equipped && target.Length == 0)
            {
                var available = CharacterArtificerEquipment.AvailableTargets(draft, slot);
                target = chosenTarget ?? character.GetSharedString(targetKey);
                if (!available.Contains(target)) target = chosenTarget == null && available.Length == 1 ? available[0] : "";
                if (target.Length == 0) return false;
            }
            draft.artificerEquippedTargets[slot] = equipped ? target : "";
            if (CharacterArtificerEquipment.ChoiceError(draft) != null)
            {
                draft.artificerEquippedTargets[slot] = before.artificerEquippedTargets[slot];
                return false;
            }

            var oldStats = CharacterCreationRules.Calculate(before);
            var newStats = CharacterCreationRules.Calculate(draft);
            var sheet = character.GetSceneData(AppConfig.Scenes.CharacterSheet);
            string Read(int index)
            {
                var field = bindings.Entries.First(e => e.key == "input." + index);
                return sheet.GetString("StableField_v1_" + field.stableId,
                    field.index < sheet.inputData.Count ? sheet.inputData[field.index] : "");
            }

            var catalog = new TranslationCatalog();
            var languages = Enum.GetValues(typeof(AppLanguage)).Cast<AppLanguage>()
                .Select(language => (Func<string, string>)(source => catalog.Translate(source, language))).ToArray();
            var oldAttacks = languages.Select(localize => GenerateAttacks(before, oldStats, bindings, localize)).ToArray();
            var newAttacks = GenerateAttacks(draft, newStats, bindings, translate);
            var currentAttacks = CreationAttackOverflow.Capture(character, bindings);
            bool missingAttacks = !character.HasSharedString(AttackVersionKey) &&
                currentAttacks.rows.All(string.IsNullOrWhiteSpace) && string.IsNullOrWhiteSpace(currentAttacks.text);
            for (int row = 0; row < 3; row++)
            {
                int start = 3 * row;
                bool matches = oldAttacks.Any(attacks => Enumerable.Range(start, 3).All(i => attacks.rows[i] == currentAttacks.rows[i]));
                if (!matches && !missingAttacks) continue;
                for (int i = start; i < start + 3; i++)
                    if (newAttacks.rows[i] != currentAttacks.rows[i]) bindings.Text(character, "input." + (66 + i), newAttacks.rows[i]);
            }

            if (oldAttacks.Any(attacks => attacks.text == currentAttacks.text) && newAttacks.text != currentAttacks.text)
            {
                bindings.Text(character, "input.75", newAttacks.text);
                character.SetSharedString("CreationGeneratedText_v1_75", CreationDescriptionLayout.Fingerprint(newAttacks.text));
                var generated = new CharacterData("artificer-attacks");
                WriteWeapons(generated, bindings, draft, translate);
                CreationAttackOverflow.Record(character, bindings, draft, newStats, "", CreationAttackOverflow.Capture(generated, bindings));
            }

            int armorChange = newStats.armorClass - oldStats.armorClass;
            if (armorChange != 0 && int.TryParse(Read(26), out int armorClass))
                bindings.Text(character, "input.26", (armorClass + armorChange).ToString());
            int speedChange = newStats.speed - oldStats.speed;
            if (speedChange != 0 && int.TryParse(Read(25), out int speed))
                bindings.Text(character, "input.25", (speed + speedChange).ToString());

            string equipment = Read(82);
            const string stamp = "CreationGeneratedText_v1_82";
            if (character.GetSharedString(stamp) == CreationDescriptionLayout.Fingerprint(equipment) ||
                languages.Any(localize => equipment == CharacterCreationPresentation.Equipment(before, localize)))
            {
                string updated = CharacterCreationPresentation.Equipment(draft, translate);
                bindings.Text(character, "input.82", updated);
                character.SetSharedString(stamp, CreationDescriptionLayout.Fingerprint(updated));
            }
            character.SetSharedString(CharacterLevelUp.DraftKey, UnityEngine.JsonUtility.ToJson(draft));
            character.SetSharedString(targetKey, target);
            character.SetSharedString(AttackVersionKey, "1");
            bindings.Toggle(character, "magic.infusion." + slot + ".prepared", equipped);
            return true;
        }

        private static CreationAttackOverflow.Baseline GenerateAttacks(CharacterCreationDraft draft, CreatedCharacterStats stats,
            CreationSheetBindings bindings, Func<string, string> translate)
        {
            var generated = new CharacterData("artificer-attacks");
            WriteWeapons(generated, bindings, draft, translate);
            CreationExpandedSpecies.WriteAttack(generated, bindings, draft, stats, translate);
            return CreationAttackOverflow.Capture(generated, bindings);
        }

        internal static bool RestoreMissingBackgroundAttacks(CharacterData character, CharacterCreationDraft draft,
            Func<int, string> read, Action<int, string> write, Func<string, string> translate)
        {
            if (!CharacterArtificerRules.Active(draft) || !CharacterArtificerRules.Revised(draft) ||
                draft.equipment != 1 || !CharacterBackgroundRules.Weapons(draft).Any() ||
                character.HasSharedString(AttackVersionKey)) return false;

            if (!Enumerable.Range(66, 10).Select(read).All(string.IsNullOrWhiteSpace)) return false;
            var generated = StartingAttacks(draft, translate);
            for (int i = 0; i < generated.rows.Length; i++)
                if (generated.rows[i].Length > 0) write(66 + i, generated.rows[i]);
            if (generated.text.Length > 0) write(75, generated.text);
            character.SetSharedString(AttackVersionKey, "1");
            return true;
        }
    }
}
