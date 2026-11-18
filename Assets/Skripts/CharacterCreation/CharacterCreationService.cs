using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    // Projects validated creation choices into a complete character.
    internal sealed class CharacterCreationService
    {
        private readonly CreationSheetBindings bindings;
        private readonly Func<string, string> translate;

        internal CharacterCreationService(CreationSheetBindings bindings, Func<string, string> translate)
        {
            this.bindings = bindings;
            this.translate = translate;
        }

        internal CharacterData Build(CharacterCreationDraft draft) => Build(draft, Array.Empty<string>());

        internal CharacterData Build(CharacterCreationDraft draft, string[] previousSpellOrder, CharacterClassHistory progression = null)
        {
            CharacterCreationValidation.ValidateChoices(draft, progression == null);
            if (bindings == null || bindings.Entries.Count < 30 || string.IsNullOrEmpty(bindings.HealthPrefix))
                throw new InvalidOperationException("Не налаштовано зв’язок майстра з карткою персонажа.");
            var secondaryTrack = draft.secondaryClass?.IsSelected == true ? draft.secondaryClass.ClassDraft(draft) : null;
            CharacterCreationValidation.ValidateSpellChoices(draft, secondaryTrack);

            CreatedCharacterStats s = CharacterCreationRules.Calculate(draft);
            var classHistory = progression ?? CharacterClassHistory.FromCreation(draft);
            if (!classHistory.MatchesDraft(draft)) throw new InvalidOperationException("Невідповідність історії рівнів персонажа.");
            int proficiency = CharacterLegacySpeciesRules.Proficiency(classHistory.TotalLevel);
            Dictionary<int, string> generated = CharacterCreationPresentation.GeneratedSheetFields(draft, s, translate);
            var c = new CharacterData(Guid.NewGuid().ToString()) { characterName = draft.characterName.Trim(), currentHealth = s.hitPoints, maxHealth = s.hitPoints };
            if (progression == null) draft.preferWideMagicResourceRows = true;
            if (draft.preferWideMagicResourceRows) c.SetSharedString(CreationMagicResources.WideRowsKey, "1");
            c.SetSharedString(CharacterLevelUp.DraftKey, JsonUtility.ToJson(draft));
            c.SetSharedString(CharacterLevelUp.ClassHistoryKey, JsonUtility.ToJson(classHistory));
            c.SetSharedString("RulesEdition", draft.edition == RulesEdition.Rules2024 ? "2024" : "2014");
            if (draft.characterClass == "warlock" || CharacterBloodHunterRules.Profane(draft)) c.SetSharedString("CreationSpellRecovery", "short");
            void Write(int index, string value) => bindings.Text(c, "input." + index, value);
            Write(57, c.characterName); Write(58, classHistory.TotalLevel.ToString()); Write(59, "0");
            foreach (int index in new[] { 54, 55, 56, 60, 61, 62, 63, 64, 65 }) Write(index, generated[index]);
            for (int i = 0; i < 6; i++)
            {
                bool saveProficient = (CharacterRangerRules.Is(draft, "gloomstalker") && draft.level >= 7 && i == 4 || CharacterRogueRules.SavingThrow(draft, i) || CharacterMonkRules.AllSavingThrows(draft) || CharacterClassCatalog.Find(draft.characterClass).savingThrows.Contains(i));
                if (secondaryTrack != null)
                    saveProficient |= CharacterMonkRules.AllSavingThrows(secondaryTrack) ||
                        CharacterRangerRules.Is(secondaryTrack, "gloomstalker") && secondaryTrack.level >= 7 && i == 4 ||
                        CharacterRogueRules.Active(secondaryTrack) && secondaryTrack.level >= 15 && (i == 4 || i == 5 && draft.edition == RulesEdition.Rules2024);
                Write(27 + 2 * i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i])));
                Write(28 + 2 * i, s.scores[i].ToString());
                var extra = secondaryTrack == null ? 0 : CharacterPaladinRules.AuraBonus(secondaryTrack) + CharacterArtificerEquipment.ProtectionBonus(secondaryTrack) +
                    (CharacterBloodHunterRules.Active(secondaryTrack) && secondaryTrack.level >= 10 && i < 3 ? Math.Max(1, CharacterBloodHunterRules.HemocraftModifier(secondaryTrack)) : 0);
                bindings.Text(c, "save." + i, CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[i]) + (saveProficient ? proficiency : 0) + CharacterPaladinRules.AuraBonus(draft) + CharacterArtificerEquipment.ProtectionBonus(draft) + (CharacterBloodHunterRules.Active(draft) && draft.level>=10 && i<3 ? Math.Max(1,CharacterBloodHunterRules.HemocraftModifier(draft)):0) + extra));
                bindings.Toggle(c, "save." + i, saveProficient);
            }
            for (int i = 0; i < 18; i++)
            {
                bindings.Text(c, "skill." + i, CharacterCreationRules.Signed(s.SkillModifiers[i]));
                bindings.Toggle(c, "skill." + i, s.skills.Contains(i));
            }
            Write(24, CharacterCreationRules.Signed(s.initiative)); Write(25, s.speed.ToString()); Write(26, s.armorClass.ToString());
            int firstHitDie = CharacterCreationRules.HitDie(draft);
            var hitDice = classHistory.HitDice().First(pool => pool.die == firstHitDie);
            Write(39, CharacterCreationRules.Signed(proficiency)); Write(40, s.passivePerception.ToString());
            Write(42, hitDice.count.ToString()); Write(43, hitDice.die.ToString());
            for (int i = 49; i <= 53; i++) Write(i, "0");
            int classGold = draft.edition == RulesEdition.Rules2024
                ? draft.characterClass == "bard" ? draft.equipment == 0 ? 19 : 90 : draft.characterClass == "paladin" ? draft.equipment == 0 ? 9 : 150 : draft.characterClass == "ranger" ? draft.equipment == 0 ? 7 : 150 : draft.characterClass == "monk" ? draft.equipment == 0 ? 11 : 50 : draft.characterClass == "barbarian" ? draft.equipment == 0 ? 15 : 75 : draft.characterClass == "rogue" ? draft.equipment == 0 ? 8 : 100 : draft.equipment == 0 ? 4 : 11
                : 0;
            if (draft.characterClass == "druid" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 9 : 50;
            if (draft.characterClass == "cleric" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 7 : 110;
            if (draft.characterClass == "wizard" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 5 : 55;
            if (draft.characterClass == "sorcerer" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 28 : 50;
            if (draft.characterClass == "warlock" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 15 : 100;
            if (draft.characterClass == "artificer" && draft.edition == RulesEdition.Rules2024) classGold = draft.equipment == 0 ? 16 : 150;
            if (CharacterFighterRules.TakesStartingGold(draft)) classGold = CharacterFighterRules.GoldOnlyAmount;
            int originGold = CharacterBackgroundRules.Gold(draft);
            Write(52, (classGold + originGold).ToString());
            var sheet = c.GetSceneData("cartaPersonaj");
            foreach (int index in new[] { 76, 77, 82 }) Write(index, generated[index]);
            for (int i = 0; i < 8; i++)
            {
                int field = i < 4 ? 78 + i : 98 + i;
                string note = generated[field];
                Write(field, note);
                if (note.Length > 0) OpenFeatureNote(sheet, i);
            }
            bool barbarian = draft.characterClass == "barbarian";
            bool rogue = draft.characterClass == "rogue";
            bool monk = draft.characterClass == "monk";
            bool paladin = draft.characterClass == "paladin";
            bool ranger = draft.characterClass == "ranger";
            bool bard = draft.characterClass == "bard";
            bool druid = draft.characterClass == "druid";
            bool cleric = draft.characterClass == "cleric";
            bool wizard = draft.characterClass == "wizard";
            bool sorcerer = draft.characterClass == "sorcerer";
            bool warlock = draft.characterClass == "warlock";
            bool artificer = draft.characterClass == "artificer";
            bool blood = draft.characterClass == "bloodhunter";
            bool pactBlade = CharacterWarlockRules.Blade(draft);
            bool hasWeapon = pactBlade || monk || (paladin || ranger || bard || druid || cleric || wizard || sorcerer || warlock || artificer ? draft.edition == RulesEdition.Rules2014 || draft.equipment == 0 : rogue ? draft.edition == RulesEdition.Rules2014 || draft.equipment == 0 : !barbarian || draft.edition == RulesEdition.Rules2014 || draft.equipment == 0);
            hasWeapon &= !CharacterFighterRules.TakesStartingGold(draft);
            int weaponMod = CharacterCreationRules.Modifier(s.scores[paladin ? 0 : ranger ? 1 : monk && draft.monkWeapon == "Велика дубина" ? 0 : monk || rogue ? 1 : barbarian ? 0 : draft.equipment == 1 ? 1 : 0]);
            if (bard)
            {
                string weapon = draft.edition == RulesEdition.Rules2024 ? "Кинджал" : draft.bardWeapon;
                weaponMod = CharacterCreationRules.Modifier(new[] { "Легкий арбалет", "Короткий лук", "Праща" }.Contains(weapon) ? s.scores[1] :
                    new[] { "Рапіра", "Кинджал", "Дротик" }.Contains(weapon) ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            }
            if (druid) weaponMod = CharacterCreationRules.Modifier(draft.edition == RulesEdition.Rules2014 && new[] { "Скімітар", "Кинджал" }.Contains(draft.druidWeapon) ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            if (cleric) weaponMod = CharacterCreationRules.Modifier(s.scores[0]);
            if (wizard) weaponMod = CharacterCreationRules.Modifier(draft.edition == RulesEdition.Rules2014 && draft.wizardWeapon == "Кинджал" ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            if (sorcerer)
            {
                string weapon = draft.edition == RulesEdition.Rules2024 ? "Спис" : draft.sorcererWeapon;
                weaponMod = CharacterCreationRules.Modifier(new[] { "Легкий арбалет", "Короткий лук", "Праща" }.Contains(weapon) ? s.scores[1] : new[] { "Кинджал", "Дротик" }.Contains(weapon) ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            }
            if (artificer)
            {
                string weapon = draft.edition == RulesEdition.Rules2024 ? "Кинджал" : draft.artificerWeapon;
                weaponMod = CharacterCreationRules.Modifier(new[] { "Легкий арбалет", "Короткий лук", "Праща" }.Contains(weapon) ? s.scores[1] : new[] { "Кинджал", "Дротик" }.Contains(weapon) ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            }
            if (warlock)
            {
                string weapon = draft.edition == RulesEdition.Rules2024 ? "Серп" : draft.warlockWeapon;
                weaponMod = pactBlade ? CharacterWarlockRules.PactModifier(draft,s.scores) : CharacterCreationRules.Modifier( new[] { "Легкий арбалет", "Короткий лук", "Праща" }.Contains(weapon) ? s.scores[1] : new[] { "Кинджал", "Дротик" }.Contains(weapon) ? Math.Max(s.scores[0], s.scores[1]) : s.scores[0]);
            }
            if (monk)
            {
                string weapon = draft.edition == RulesEdition.Rules2024 ? draft.equipment == 0 ? "Спис" : "Беззбройний удар" : draft.equipment == 0 ? "Короткий меч" : draft.monkWeapon;
                weaponMod = CharacterMonkRules.WeaponModifier(draft, s.scores, weapon);
            }
            if (blood) weaponMod = CharacterBloodHunterRules.WeaponModifier(draft, s.scores);
            if (rogue) weaponMod = CharacterCreationRules.Modifier(Math.Max(s.scores[0], s.scores[1]));
            Write(67, generated[67]);
            bool weaponProficient = !druid || draft.edition == RulesEdition.Rules2024 || !new[] { "Велика дубина", "Ручна сокира", "Легкий молот" }.Contains(draft.druidWeapon);
            if(druid&&draft.edition==RulesEdition.Rules2014&&CharacterWeaponCatalog.Find(draft.druidWeapon,draft.edition) is CoreWeapon druidWeapon)weaponProficient=CreationStartingWeapons.Proficient(draft,druidWeapon);
            if (sorcerer) weaponProficient = CharacterSorcererRules.WeaponProficient(draft, draft.sorcererWeapon);
            Write(66, hasWeapon ? CharacterCreationRules.Signed(weaponMod + (weaponProficient ? proficiency : 0) + ((draft.characterClass == "fighter" && draft.equipment == 1 || ranger && draft.level >= 2) && CharacterFighterRules.HasStyle(draft, FighterStyle.Archery) ? 2 : 0)) : "");
            Write(68, !hasWeapon || blood && draft.bloodWeapon == "Сітка" ? "" : blood ? CharacterBloodHunterRules.Damage(draft.bloodWeapon) + CharacterCreationRules.Signed(weaponMod) : artificer ? (draft.edition == RulesEdition.Rules2024 ? "1d4" : CharacterCreationRules.MonkWeaponDamage(draft.artificerWeapon)) + CharacterCreationRules.Signed(weaponMod) : warlock ? (pactBlade ? CharacterWarlockRules.PactDamage[draft.pactWeapon] : draft.edition == RulesEdition.Rules2024 ? "1d4" : CharacterCreationRules.MonkWeaponDamage(draft.warlockWeapon)) + CharacterCreationRules.Signed(weaponMod) : sorcerer ? (draft.edition == RulesEdition.Rules2024 ? "1d6" : CharacterCreationRules.MonkWeaponDamage(draft.sorcererWeapon)) + CharacterCreationRules.Signed(weaponMod) : wizard ? (draft.edition == RulesEdition.Rules2014 && draft.wizardWeapon == "Кинджал" ? "1d4" : "1d6") + CharacterCreationRules.Signed(weaponMod) : cleric ? (draft.edition == RulesEdition.Rules2014 && draft.clericWeapon == "Бойовий молот" ? "1d8" : "1d6") + CharacterCreationRules.Signed(weaponMod) : druid
                ? (draft.edition == RulesEdition.Rules2024 ? "1d4" : draft.druidWeapon == "Скімітар" ? "1d6" : CharacterCreationRules.MonkWeaponDamage(draft.druidWeapon)) + CharacterCreationRules.Signed(weaponMod)
                : bard
                ? (draft.edition == RulesEdition.Rules2024 ? "1d4" : new[] { "Рапіра", "Довгий меч" }.Contains(draft.bardWeapon) ? "1d8" : CharacterCreationRules.MonkWeaponDamage(draft.bardWeapon)) + CharacterCreationRules.Signed(weaponMod)
                : monk
                ? CharacterMonkRules.WeaponDamage(draft, draft.edition == RulesEdition.Rules2024 ? draft.equipment == 1 ? "Беззбройний удар" : "Спис" : draft.equipment == 1 ? draft.monkWeapon : "Короткий меч") + CharacterCreationRules.Signed(weaponMod)
                : ranger ? "1d8" + CharacterCreationRules.Signed(weaponMod)
                : paladin ? "1d8" + CharacterCreationRules.Signed(weaponMod)
                : rogue
                ? (draft.edition == RulesEdition.Rules2014 && draft.equipment == 0 ? "1d8" : "1d6") + CharacterCreationRules.Signed(weaponMod)
                : barbarian
                ? (draft.equipment == 0 ? "1d12" : "1d8") + CharacterCreationRules.Signed(weaponMod)
                : (draft.equipment == 1 ? "1d8" : draft.edition == RulesEdition.Rules2024 ? "2d6" : "1d8") + CharacterCreationRules.Signed(weaponMod + (draft.equipment == 0 && draft.edition == RulesEdition.Rules2014 && CharacterFighterRules.HasStyle(draft, FighterStyle.Dueling) ? 2 : 0)));
            if (ranger && hasWeapon) {
                int melee = CharacterCreationRules.Modifier(Math.Max(s.scores[0], s.scores[1]));
                Write(69, CharacterCreationRules.Signed(proficiency + melee));
                Write(70, translate(draft.edition == RulesEdition.Rules2024 ? "Скімітар" : "Короткий меч")); Write(71, "1d6" + CharacterCreationRules.Signed(melee));
                Write(72, CharacterCreationRules.Signed(proficiency + melee)); Write(73, translate("Короткий меч"));
                Write(74, "1d6" + (draft.level >= 2 && draft.fightingStyle == FighterStyle.TwoWeapon || melee < 0 ? CharacterCreationRules.Signed(melee) : ""));
            }
            Write(75, generated[75]);
            if (monk)
            {
                if (draft.level >= 2)
                {
                    string monkModifier = CharacterCreationRules.Signed(CharacterCreationRules.Modifier(s.scores[4]));
                    string monkDc = (8 + proficiency + CharacterCreationRules.Modifier(s.scores[4])).ToString();
                    Write(44, monkModifier); Write(48, monkDc);
                    c.SetSharedString("SharedInput_magMod", monkModifier); c.SetSharedString("SharedInput_slogSpas", monkDc);
                }
                int unarmed = Math.Max(CharacterCreationRules.Modifier(s.scores[0]), CharacterCreationRules.Modifier(s.scores[1]));
                if (draft.edition == RulesEdition.Rules2014 || draft.equipment == 0)
                {
                    Write(69, CharacterCreationRules.Signed(proficiency + unarmed));
                    Write(70, translate("Беззбройний удар"));
                    Write(71, CharacterMonkRules.MartialDie(draft) + CharacterCreationRules.Signed(unarmed));
                }
                if (CharacterMonkRules.Points(draft) > 0)
                {
                    bindings.Dropdown(c, "main.ki.count", CharacterMonkRules.Points(draft));
                    CreationResourceRecovery.Register(c, bindings, "rest.main.ki", true);
                    if (draft.edition == RulesEdition.Rules2024)
                        CreationMagicResources.Add(c, bindings, translate("Незвичайний метаболізм"), 1, false);
                }
            }
            if (barbarian) bindings.Dropdown(c, "main.rage.count", CharacterBarbarianRules.UnlimitedRage(draft) ? 7 : CharacterBarbarianRules.Rages(draft));
            if (rogue || secondaryTrack?.characterClass == "rogue")
                Write(46, CharacterRogueRules.SneakDie(rogue ? draft : secondaryTrack));
            // The class selector displays the existing Rage panel for Barbarian.
            if (draft.characterClass == "fighter" || paladin || ranger && draft.edition == RulesEdition.Rules2024) Write(101, generated[101]);
            if (sorcerer) {
                bindings.Dropdown(c, "main.sorcery.count", CharacterSorcererRules.Points(draft));
                bindings.Toggle(c, "rest.main.sorcery", false);
                int shortRecovery = draft.edition == RulesEdition.Rules2024 ? draft.level >= 5 ? draft.level / 2 : 0 : draft.level >= 20 ? 4 : 0;
                CreationResourceRecovery.RegisterPartial(c, bindings, "rest.main.sorcery", shortRecovery, draft.edition == RulesEdition.Rules2024);
            }
            int resource = draft.edition == RulesEdition.Rules2024 && draft.species == "human" ? 10 :
                draft.edition == RulesEdition.Rules2024 && CharacterSpeciesRules.IsDwarf(draft) ? 9 :
                (draft.species == "dragonborn"||CharacterFtdDragonbornRules.Active(draft)) ? 8 : monk && draft.level >= 2 ? 4 : blood ? 6 : cleric && draft.level>=2 ? 3 : druid && draft.level>=2 ? 2 : barbarian ? 1 : sorcerer && draft.level >= 2 ? 5 : paladin ? 11 : ranger && draft.edition == RulesEdition.Rules2024 ? 12 : rogue || monk || ranger || bard || druid || cleric || wizard || sorcerer || warlock || artificer ? 0 : 12;
            SetDropdown(sheet, 14, resource);
            sheet.SetInt("StableField_v1_dnd.cartapersonaj.dropdown.dropdown-legacy-1.2c571c21be8b", resource);
            sheet.SetInt("ResourceDropdownOrder_v2", 1);
            sheet.SetInt("ResourceDropdownOrder_v3", 1);
            if (draft.characterClass == "fighter") SetDropdown(sheet, 13, s.secondWindUses);
            if (paladin)
            {
                SetDropdown(sheet, 13, 0);
                c.SetSharedString("CreationLayOnHandsMaximum", (5 * draft.level).ToString());
                bindings.Text(c, "main.lay.points", (5 * draft.level).ToString());
                CreationResourceRecovery.RegisterPoints(c, bindings, "main.lay.points", 5 * draft.level);
            }
            if (ranger && draft.edition == RulesEdition.Rules2024) SetDropdown(sheet, 13, CharacterRangerRules.MarkUses(draft));
            if (draft.edition == RulesEdition.Rules2024 && CharacterSpeciesRules.IsDwarf(draft))
            {
                sheet.SetInt("StableField_v1_creation.stonecunning.count", proficiency);
            }
            if (draft.edition == RulesEdition.Rules2024 && draft.species == "human")
            {
                sheet.SetInt("StableField_v1_creation.heroic.count", 1);
            }
            if (draft.characterClass == "fighter") CreationResourceRecovery.Register(c, bindings, "rest.main.other", true, draft.edition == RulesEdition.Rules2024);
            else if (ranger && draft.edition == RulesEdition.Rules2024) CreationResourceRecovery.Register(c, bindings, "rest.main.other", false);
            else if (barbarian && !CharacterBarbarianRules.UnlimitedRage(draft)) CreationResourceRecovery.Register(c, bindings, "rest.main.rage", draft.edition == RulesEdition.Rules2024, draft.edition == RulesEdition.Rules2024);
            else if (blood) CreationResourceRecovery.Register(c, bindings, "rest.main.blood", true);
            if (draft.edition == RulesEdition.Rules2024 && CharacterSpeciesRules.IsDwarf(draft)) CreationResourceRecovery.Register(c, bindings, "rest.main.stone", false);
            if (draft.edition == RulesEdition.Rules2024 && draft.species == "human") CreationResourceRecovery.Register(c, bindings, "rest.main.heroic", false);
            if (draft.species == "dragonborn"||CharacterFtdDragonbornRules.Active(draft))
            {
                Write(47, CharacterFtdDragonbornRules.Active(draft)?CharacterFtdDragonbornRules.Dice(draft):CharacterLegacySpeciesRules.BreathDice(draft));
                bindings.Dropdown(c, "main.dragon.count", draft.edition == RulesEdition.Rules2024||CharacterFtdDragonbornRules.Active(draft) ? CharacterLegacySpeciesRules.Proficiency(draft) : 1);
                CreationResourceRecovery.Register(c, bindings, "rest.main.dragon", draft.edition == RulesEdition.Rules2014&&!CharacterFtdDragonbornRules.Active(draft));
            }
            int arrows = (ranger ? draft.edition == RulesEdition.Rules2014 || draft.equipment == 0 ? 20 : 0 : rogue ? draft.equipment == 0 ? 20 : 0 : !barbarian && !paladin && !bard && !druid && !cleric && !wizard && !sorcerer && !warlock && !artificer && !blood && draft.equipment == 1 ? 20 : 0) +
                (draft.edition == RulesEdition.Rules2024 && (draft.background == "soldier" || draft.background == "guide") && draft.backgroundEquipmentChoice == 0 ? 20 : 0);
            if (arrows > 0) Write(93, arrows.ToString());
            if (cleric && draft.edition == RulesEdition.Rules2014 && draft.clericSecondWeapon == "Легкий арбалет") Write(94, "20");
            if (sorcerer && draft.edition == RulesEdition.Rules2014 && draft.sorcererWeapon == "Легкий арбалет") Write(94, "20");
            if (warlock && draft.edition == RulesEdition.Rules2014 && draft.warlockWeapon == "Легкий арбалет") Write(94, "20");
            if (artificer && draft.edition == RulesEdition.Rules2014 || blood) Write(94, "20");
            if (blood)
            {
                Write(45, "1d" + CharacterBloodHunterRules.Die(draft));
                c.SetSharedString("CreationBloodDieBinding_v2", "1");
                int hemocraft = CharacterCreationRules.Modifier(s.scores[draft.bloodAbility]);
                string modifier = CharacterCreationRules.Signed(hemocraft), dc = (8 + proficiency + hemocraft).ToString();
                Write(44, modifier); Write(48, dc);
                c.SetSharedString("SharedInput_magMod", modifier); c.SetSharedString("SharedInput_slogSpas", dc);
                SetDropdown(sheet, 11, CharacterBloodHunterRules.CurseUses(draft));
                sheet.SetInt("StableField_v1_dnd.cartapersonaj.dropdown.dropdown-legacy-2.101a7444bdf8", CharacterBloodHunterRules.CurseUses(draft));
                Write(70, translate("Легкий арбалет"));
                int dexterity = CharacterCreationRules.Modifier(s.scores[1]);
                Write(69, CharacterCreationRules.Signed(proficiency + dexterity + (draft.level>=2 && draft.fightingStyle==FighterStyle.Archery?2:0))); Write(71, "1d8" + CharacterCreationRules.Signed(dexterity));
                if (draft.bloodWeaponMode == 1)
                {
                    int secondMod = CharacterBloodHunterRules.WeaponModifier(draft.bloodSecondWeapon, s.scores);
                    Write(73, translate(draft.bloodSecondWeapon)); Write(72, CharacterCreationRules.Signed(proficiency + secondMod));
                    Write(74, CharacterBloodHunterRules.Damage(draft.bloodSecondWeapon) + CharacterCreationRules.Signed(secondMod));
                }
            }
            if (warlock && draft.edition == RulesEdition.Rules2014 && draft.warlockWeapon == "Легкий арбалет") Write(94, "20");
            if (monk && draft.edition == RulesEdition.Rules2014) Write(96, "10"); // darts
            if (draft.edition == RulesEdition.Rules2024 && draft.background == "guard" && draft.backgroundEquipmentChoice == 0)
                Write(94, ((sheet.inputData.Count > 94 && int.TryParse(sheet.inputData[94], out int bolts) ? bolts : 0) + 20).ToString());
            var (spellSlots, separatePact) = CharacterSpellSlotProgression.ForSheet(draft);
            if (separatePact.Length > 0) c.SetSharedString("CreationSpellRecovery", "long");
            else if (secondaryTrack != null && (CharacterWarlockRules.Active(secondaryTrack) || CharacterBloodHunterRules.Profane(secondaryTrack)))
                c.SetSharedString("CreationSpellRecovery", "short");
            for (int circle = 0; circle < spellSlots.Length; circle++)
                bindings.Dropdown(c, "magic.slot." + circle, spellSlots[circle]);
            var magicFeats = CharacterOriginFeatRules.Choices(draft).Where(CharacterOriginFeatRules.Magic).ToArray();
            new CreationSpellbookWriter(bindings, translate).Write(c, draft, secondaryTrack, s, previousSpellOrder, magicFeats);
            if (wizard) CreationWizardMagic.WriteResources(c, bindings, draft, translate);
            if(warlock)CreationWarlockResources.Write(c,bindings,draft,s,translate);
            if (CharacterRogueRules.Is(draft, "soulknife"))
            {
                int dex = CharacterCreationRules.Modifier(Math.Max(s.scores[0], s.scores[1]));
                Write(69, CharacterCreationRules.Signed(proficiency + dex)); Write(70, translate("Психічний клинок")); Write(71, "1d6" + CharacterCreationRules.Signed(dex));
                Write(72, CharacterCreationRules.Signed(proficiency + dex)); Write(73, translate("Другий психічний клинок")); Write(74, "1d4" + CharacterCreationRules.Signed(dex));
            }
            CreationClassMagicResources.Write(c, bindings, draft, s, translate, hasWeapon, weaponMod);
            if (CharacterLevelAdvancement.HasBoon(draft, "fate")) CreationMagicResources.Add(c, bindings, translate("Дар долі"), 1, true);
            if (CharacterLevelAdvancement.HasBoon(draft, "recovery"))
            {
                CreationMagicResources.Add(c, bindings, translate("Остання стійкість"), 1, false);
                CreationMagicResources.Add(c, bindings, translate("Кубики відновлення 1–5"), 5, false);
                CreationMagicResources.Add(c, bindings, translate("Кубики відновлення 6–10"), 5, false);
            }
            foreach (string freeOriginSpell in CharacterSpeciesRules.FreeSpells(draft))
                CreationMagicResources.Add(c, bindings, translate(freeOriginSpell), CharacterSpeciesRules.FreeSpellUses(draft), false);
            if (draft.edition == RulesEdition.Rules2024 && draft.TotalLevel >= 5)
            {
                if (draft.species == "dragonborn") CreationMagicResources.Add(c, bindings, translate("Драконячий політ"), 1, false);
                if (draft.species == "goliath") CreationMagicResources.Add(c, bindings, translate("Велика форма"), 1, false);
            }
            if (draft.species == "halforc" || draft.species == "orc")
            {
                CreationMagicResources.Add(c, bindings, translate("Невблаганна витривалість"), 1, false);
                if (draft.species == "orc") CreationMagicResources.Add(c, bindings, translate("Приплив адреналіну"), proficiency, true);
            }
            if (draft.species == "goliath") CreationMagicResources.Add(c, bindings, translate(CharacterLegacySpeciesRules.GiantPower(draft)), CharacterLegacySpeciesRules.Proficiency(draft), false);
            if (draft.species == "aasimar")
            {
                CreationMagicResources.Add(c, bindings, translate("Цілющі руки"), 1, false);
                if (draft.TotalLevel >= 3) CreationMagicResources.Add(c, bindings, translate("Небесне одкровення"), 1, false);
            }
            foreach (var feat in magicFeats) CreationMagicResources.Add(c, bindings, string.Format(translate("Риса: {0}"), translate(feat.spell)), 1, false);
            if (CharacterOriginFeatRules.Has(draft, "lucky")) CreationMagicResources.Add(c, bindings, translate("Очки везіння"), CharacterLegacySpeciesRules.Proficiency(draft), false);
            sheet.SetInt(bindings.HealthPrefix + "maxHealth", s.hitPoints);
            sheet.SetInt(bindings.HealthPrefix + "currentHealth", s.hitPoints);
            CreationArtificerResources.Write(c, bindings, draft, translate);
            CreationBloodHunterResources.Write(c,bindings,draft,translate);
            CreationClericResources.Write(c,bindings,draft,translate);
            CreationDruidResources.Write(c,bindings,draft,translate);
            var attackBaseline=CreationAttackOverflow.Capture(c,bindings);
            CreationStartingWeapons.Write(c,bindings,draft,s,translate);
            CreationExpandedSpecies.Write(c,bindings,draft,s,translate);
            foreach (var pool in separatePact)
            {
                string restKey = CreationMagicResources.Add(c, bindings,
                    string.Format(translate("Комірки договору — коло {0}"), pool.circle), pool.count, true, useLastRow: true);
                c.SetSharedString("CreationPactResourceRow_v1", restKey);
            }
            CreationMulticlassResources.Write(c, bindings, draft, translate);
            CreationMagicResources.WriteNotes(c, bindings, draft, s.features, translate);
            CreationAttackOverflow.Record(c,bindings,draft,s,generated[75],attackBaseline);
            foreach(var field in generated.Where(field=>CreationDescriptionLayout.Tracks(field.Key)))
                c.SetSharedString("CreationGeneratedText_v1_"+field.Key,CreationDescriptionLayout.Fingerprint(field.Value));
            // Ready forms remain in the asset catalogue until the player adds one.
            return c;
        }

        internal static void OpenFeatureNote(CharacterSceneData sheet, int note)
        {
            SetToggle(sheet, note < 4 ? 31 + note : 143 + note, true);
            string stableId = note switch
            {
                0 => "toggle.67f2930e0f12",
                1 => "toggle.dc28c59a7549",
                2 => "toggle.d251d8eb9f69",
                3 => "toggle.bd328dd74535",
                _ => "creationnotes" + (note + 1) + ".20261027"
            };
            sheet.SetInt("StableField_v1_dnd.cartapersonaj.toggle." + stableId, 1);
        }

        private static void SetDropdown(CharacterSceneData data, int index, int value)
        {
            while (data.dropdownData.Count <= index) data.dropdownData.Add(0);
            data.dropdownData[index] = value;
        }
        private static void SetToggle(CharacterSceneData data, int index, bool value)
        {
            while (data.toggleData.Count <= index) data.toggleData.Add(false);
            data.toggleData[index] = value;
        }

    }
}
