using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal sealed class CreationSpellbookWriter
    {
        private readonly CreationSheetBindings bindings;
        private readonly Func<string, string> translate;

        internal CreationSpellbookWriter(CreationSheetBindings bindings, Func<string, string> translate)
        {
            this.bindings = bindings;
            this.translate = translate;
        }

        internal void Write(CharacterData c, CharacterCreationDraft draft, CharacterCreationDraft secondaryTrack,
            CreatedCharacterStats s, string[] previousSpellOrder, CharacterOriginFeatChoice[] magicFeats)
        {
            bool hasClassSpells = CharacterCreationRules.SpellChoiceCount(draft) > 0;
            string[] secondarySpells = secondaryTrack == null ? Array.Empty<string>() : CreationSpellPreparation.ClassSpells(secondaryTrack).ToArray();
            bool paladin = CharacterPaladinRules.Active(draft);
            bool ranger = CharacterRangerRules.Active(draft);
            bool bard = CharacterBardRules.Active(draft);
            bool druid = CharacterDruidRules.Active(draft);
            bool cleric = CharacterClericRules.Active(draft);
            bool wizard = CharacterWizardRules.Active(draft);
            bool sorcerer = CharacterSorcererRules.Active(draft);
            bool warlock = CharacterWarlockRules.Active(draft);
            bool artificer = CharacterArtificerRules.Active(draft);
            bool blood = CharacterBloodHunterRules.Active(draft);
            int proficiency = CharacterLegacySpeciesRules.Proficiency(draft);
            void Write(int index, string value) => bindings.Text(c, "input." + index, value);

            string[] originSpells = CharacterSpeciesRules.Spells(draft);
            string[] monkSpells = CharacterMonkSubclassRules.Spells(draft);
            string[] barbarianSpells = CharacterBarbarianRules.Spells(draft);
            bool trickster = CharacterTricksterSpells.Active(draft);
            bool eldritch = CharacterFighterRules.Caster(draft);
            bool telekinetic = CharacterFighterRules.Is(draft, "psiwarrior") && draft.level >= 18;
            string[] featSpells = magicFeats.SelectMany(CharacterOriginFeatRules.Spells).Distinct().ToArray();
            if (hasClassSpells || originSpells.Length > 0 || featSpells.Length > 0 || monkSpells.Length > 0 || barbarianSpells.Length > 0 || telekinetic || secondarySpells.Length > 0)
            {
                int spellAbility = hasClassSpells || telekinetic ? CreationSpellPreparation.ClassAbility(draft) :
                    secondarySpells.Length > 0 ? CreationSpellPreparation.ClassAbility(secondaryTrack) :
                    barbarianSpells.Length > 0 || monkSpells.Length > 0 ? 4 :
                    originSpells.Length > 0 ? CharacterSpeciesRules.MagicAbility(draft) : magicFeats[0].ability;
                int spellModifier = CharacterCreationRules.Modifier(s.scores[spellAbility]);
                string modifierText = CharacterCreationRules.Signed(spellModifier);
                string saveText = (8 + proficiency + spellModifier).ToString();
                Write(44, modifierText); Write(48, saveText);
                c.SetSharedString("SharedInput_magMod", modifierText);
                c.SetSharedString("SharedInput_slogSpas", saveText);
                var spellNames = (wizard ? CharacterWizardRules.Book(draft) : draft.preparedSpells).Concat(draft.cantrips).ToList();
                spellNames.AddRange(CharacterWizardRules.Automatic(draft));
                if (ranger && draft.edition == RulesEdition.Rules2024 && !spellNames.Contains("Hunter's Mark")) spellNames.Add("Hunter's Mark");
                if (druid && draft.edition == RulesEdition.Rules2024 && !spellNames.Contains("Speak with Animals")) spellNames.Add("Speak with Animals");
                spellNames.AddRange(CharacterClericRules.DomainSpells(draft));
                spellNames.AddRange(CharacterDruidRules.AutomaticSpells(draft));spellNames.AddRange(CharacterDruidRules.FeatureSpells(draft));
                spellNames.AddRange(CharacterPaladinRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterRangerRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterSorcererRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterBardRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterArtificerRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterArtificerRules.FeatureSpells(draft));
                spellNames.AddRange(CharacterBloodHunterRules.AutomaticSpells(draft));
                if(bard)spellNames.AddRange(draft.bardSecrets);
                if (artificer && draft.edition == RulesEdition.Rules2024) spellNames.Add("Mending");
                if (CharacterClericRules.Classic(draft) && draft.subclass == "light" && !spellNames.Contains("Light")) spellNames.Add("Light");
                if (CharacterClericRules.Classic(draft) && draft.subclass == "nature" && !spellNames.Contains(draft.natureCantrip)) spellNames.Add(draft.natureCantrip);
                if (CharacterWarlockRules.Tome(draft)) { spellNames.AddRange(draft.tomeCantrips); spellNames.AddRange(draft.tomeRituals); }
                spellNames.AddRange(CharacterWarlockRules.AutomaticSpells(draft));
                spellNames.AddRange(CharacterWarlockInvocations.FeatureSpells(draft));
                if (trickster) spellNames.Add("Mage Hand");
                if (telekinetic) spellNames.Add("Telekinesis");
                spellNames.AddRange(monkSpells);
                spellNames.AddRange(barbarianSpells);
                spellNames.AddRange(originSpells);
                spellNames.AddRange(featSpells);
                var entries = spellNames.Distinct(StringComparer.Ordinal)
                    .Select(name => (key: name, name, secondary: false))
                    .Concat(secondarySpells.Select(name => (key: secondaryTrack.characterClass + "|" + name, name, secondary: true)))
                    .ToDictionary(entry => entry.key, StringComparer.Ordinal);
                var order = previousSpellOrder.Where(entries.ContainsKey).Concat(entries.Keys.Except(previousSpellOrder)).ToArray();
                c.SetSharedString(CharacterLevelUp.SpellOrderKey, string.Join("\n", order));
                if (order.Length > 75) throw new InvalidOperationException("Книга заклинань заповнена.");
                for (int i = 0; i < order.Length; i++)
                {
                    var entry = entries[order[i]];
                    string spellName = entry.name;
                    bool secondaryCard = entry.secondary;

                    string name = translate(spellName);
                    if (secondaryCard)
                        name += " · " + translate(CharacterClassCatalog.Find(secondaryTrack.characterClass).name);
                    var grantingFeat = magicFeats.FirstOrDefault(feat => CharacterOriginFeatRules.Spells(feat).Contains(spellName));
                    bool classSource = !secondaryCard && CreationSpellPreparation.UsesClassAbility(draft, spellName);
                    var owner = secondaryCard ? secondaryTrack : draft;
                    int castingAbility = classSource?CreationSpellPreparation.ClassAbility(draft):grantingFeat != null ? grantingFeat.ability : CharacterMonkSubclassRules.Spells(owner).Contains(spellName) ? 4 : originSpells.Contains(spellName) ? CharacterSpeciesRules.MagicAbility(draft) : paladin ? 5 : ranger ? 4 : sorcerer || bard ? 5 : artificer ? 3 : cleric || druid ? 4 : blood ? draft.bloodAbility : -1;
                    if (secondaryCard) castingAbility = CreationSpellPreparation.ClassAbility(secondaryTrack);
                    int descriptionLevel = owner != draft ? owner.level : bard || artificer || cleric || druid || warlock || wizard ? draft.level : 1;
                    string description = CharacterSpellCatalog.CardDescription(draft.edition, spellName, owner.characterClass, translate, castingAbility, (CharacterDruidRules.Active(owner)&&CharacterDruidRules.Revised(owner)&&spellName=="Find Familiar") || (CharacterRangerRules.Is(owner, "feywanderer") && owner.level >= 11 && spellName == "Summon Fey") || (CharacterSorcererRules.Is(owner, "draconic") && owner.level >= 18 && spellName == "Summon Dragon"), descriptionLevel);
                    if (!secondaryCard && wizard)
                    {
                        string note = CreationWizardMagic.CardNote(draft, spellName, translate);
                        if (note.Length > 0) description += "\n" + note;
                    }
                    if(!secondaryCard && warlock){string note=CreationWarlockResources.SpellNote(draft,spellName,translate);if(note.Length>0)description+="\n"+note;}
                    if(!secondaryCard&&(draft.species=="githyanki"||draft.species=="githzerai")&&CharacterExpandedSpeciesRules.Spells(draft).Contains(spellName))
                    {
                        description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.gith"));
                        if(spellName=="Mage Hand")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.githhand"));
                    }
                    if(!secondaryCard&&draft.species=="rimekin"&&spellName=="Flame Blade")description=translate(CharacterExpandedSpeciesRules.Text("spell.coldblade"))+"\n\n"+description;
                    if(!secondaryCard&&draft.species=="yuanti"&&spellName=="Animal Friendship")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.snakes"));
                    if(!secondaryCard&&draft.species=="genasi"&&CharacterExpandedSpeciesRules.FreeSpells(draft).Contains(spellName)&&(draft.speciesLineage=="air"||draft.speciesLineage=="earth"||spellName=="Flame Blade"||spellName=="Water Walk"))description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.genasi"));
                    if(!secondaryCard&&draft.species=="genasi"&&draft.speciesLineage=="earth"&&spellName=="Blade Ward")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.earthward"));
                    if(!secondaryCard&&draft.species=="duergar"&&CharacterExpandedSpeciesRules.FreeSpells(draft).Contains(spellName))description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.duergar"));
                    if(!secondaryCard&&draft.species=="deepgnome"&&spellName=="Nondetection")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.nondetection"));
                    if(!secondaryCard&&draft.species=="firbolg"&&spellName=="Disguise Self")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.height"));
                    if(!secondaryCard&&draft.species=="aarakocra"&&spellName=="Gust of Wind")description+="\n"+translate(CharacterExpandedSpeciesRules.Text("spell.wind"));
                    if (CharacterRangerRules.Is(owner, "feywanderer") && owner.level >= 11 && spellName == "Summon Fey") description += "\n" + translate(CharacterRangerText.Get("fey11"));
                    if (ranger && draft.level >= 20 && CharacterRangerRules.Revised(draft) && spellName == "Hunter's Mark") description += "\n" + translate(CharacterRangerText.Get("slayer24"));
                    if (CharacterSorcererRules.Is(owner, "draconic") && owner.level >= 18 && spellName == "Summon Dragon") description += "\n" + translate(CharacterSorcererText.Get("dragon.18.24"));
                    if (CharacterSorcererRules.Is(owner, "aberrant") && owner.level >= 6 && CharacterSorcererRules.AutomaticSpells(owner).Contains(spellName) && (CharacterSpellCatalog.Find(draft.edition, spellName)?.level ?? 0) > 0) description += "\n" + translate(CharacterSorcererText.Get("aberrant.6"));
                    bool racialRebuke=!secondaryCard&&draft.species=="tiefling"&&draft.edition==RulesEdition.Rules2014&&spellName=="Hellish Rebuke";
                    if (racialRebuke&&!classSource)
                        description = translate("Пекельна відсіч спадщини: реакція на видимого кривдника у 60 футах; ряткидок Спритності проти 3к10 вогняної шкоди, успіх — половина. Накладання як спел 2 рівня.");
                    if(racialRebuke&&classSource)description+="\n"+translate(CharacterDescriptionText.Get("spell.rebuke.racial"));
                    if (CharacterClericRules.Classic(owner) && owner.subclass == "life" && (spellName == "Cure Wounds" || spellName == "Healing Word")) description += "\n" + translate("Учень життя: додатково +3 ОЗ при цьому накладанні.");
                    if (CharacterMonkSubclassRules.Spells(owner).Contains(spellName))
                    {
                        string use = CharacterMonkSubclassRules.SpellUse(owner, spellName);
                        if (CharacterMonkSubclassRules.ClassicElements(owner) && new[] { "Stoneskin", "Gaseous Form", "Fly" }.Contains(spellName))
                            use += "\n" + translate("Дисципліна: лише на себе.");
                        description += "\n" + CharacterCreationPresentation.FeatureLines(use, translate);
                        int modifier = CharacterCreationRules.Modifier(s.scores[4]);
                        description += "\n" + string.Format(translate("Магія монаха: атака {0}; СК {1}."), CharacterCreationRules.Signed(proficiency + modifier), 8 + proficiency + modifier);
                    }
                    if (!secondaryCard && originSpells.Contains(spellName))
                    {
                        int ability = CharacterSpeciesRules.MagicAbility(draft);
                        int modifier = CharacterCreationRules.Modifier(s.scores[ability]);
                        description += "\n" + string.Format(translate("Спадкова магія: {0}; атака {1}; СК {2}."), translate(CharacterCreationRules.AbilityNames[ability]), CharacterCreationRules.Signed(proficiency + modifier), 8 + proficiency + modifier);
                        if (CharacterSpeciesRules.FreeSpells(draft).Contains(spellName)) description += "\n" + string.Format(translate("Накладань без комірки за довгий відпочинок: {0}."), CharacterSpeciesRules.FreeSpellUses(draft));
                    }
                    foreach (var feat in magicFeats.Where(feat => !secondaryCard && CharacterOriginFeatRules.Spells(feat).Contains(spellName)))
                    {
                        int modifier = CharacterCreationRules.Modifier(s.scores[feat.ability]);
                        description += "\n" + string.Format(translate("Магія риси: {0}; атака {1}; СК {2}."), translate(CharacterCreationRules.AbilityNames[feat.ability]), CharacterCreationRules.Signed(proficiency + modifier), 8 + proficiency + modifier);
                        if (spellName == feat.spell) description += "\n" + translate("Завжди підготовлене; одне накладання без комірки за довгий відпочинок, також можна витрачати комірки.");
                    }
                    var spell = CharacterSpellCatalog.Find(draft.edition, spellName) ?? throw new InvalidOperationException("Missing spell metadata: " + spellName);
                    bool prepared = CreationSpellPreparation.IsPrepared(owner, spellName, spell.level);
                    int arcanaIndex = Array.IndexOf(CharacterBloodHunterRules.AutomaticSpells(owner), spellName);
                    if (arcanaIndex >= 0)
                    {
                        string arcana = CharacterBloodHunterRules.Text(arcanaIndex == 0 ? "profane.revealed" : "profane.unsealed");
                        description += "\n\n" + string.Format(translate(arcana), translate(spellName));
                    }
                    if (CharacterArtificerRules.Active(owner))
                    {
                        if (spell.level > 0 && CharacterArtificerRules.AutomaticSpells(owner).Contains(spellName))
                            description += "\n\n" + translate("Завжди підготовлене завдяки підкласу Винахідника.");
                        if (owner.level >= 11 && spellName == owner.artificerStoredSpell)
                            description += "\n\n" + translate("Збережене у предметі. Накладання з предмета витрачає використання Сховища заклинання.");
                        else if (!prepared && CharacterArtificerRules.FeatureSpells(owner).Contains(spellName))
                            description += "\n\n" + translate("Доступне через здібність Винахідника. Умови накладання вказані в нотатках магії.");
                    }
                    if (CharacterBarbarianRules.Spells(owner).Contains(spellName)) description += "\n" + translate("Лише ритуалом; не можна накладати під час Люті. Мудрість.");
                    if (secondaryCard)
                    {
                        int ability = CreationSpellPreparation.ClassAbility(secondaryTrack);
                        int modifier = CharacterCreationRules.Modifier(s.scores[ability]);
                        description += "\n" + translate(CharacterClassCatalog.Find(secondaryTrack.characterClass).name) + ". " +
                            string.Format(translate("Магія риси: {0}; атака {1}; СК {2}."),
                                translate(CharacterCreationRules.AbilityNames[ability]), CharacterCreationRules.Signed(proficiency + modifier), 8 + proficiency + modifier);
                        string classNote = CharacterWizardRules.Active(secondaryTrack)
                            ? CreationWizardMagic.CardNote(secondaryTrack, spellName, translate)
                            : CharacterWarlockRules.Active(secondaryTrack)
                                ? CreationWarlockResources.SpellNote(secondaryTrack, spellName, translate) : "";
                        if (classNote.Length > 0) description += "\n" + classNote;
                    }
                    if (!secondaryCard && eldritch && (draft.cantrips.Contains(spellName) || draft.preparedSpells.Contains(spellName))) description += "\n" + string.Format(translate("Магія лицаря: атака {0}; СК {1} (Інтелект)."), CharacterCreationRules.Signed(proficiency + CharacterCreationRules.Modifier(s.scores[3])), 8 + proficiency + CharacterCreationRules.Modifier(s.scores[3]));
                    if (CharacterFighterRules.Is(owner, "psiwarrior") && owner.level >= 18 && spellName == "Telekinesis") description += "\n" + translate(CharacterFighterText.Get("telekinetic"));
                    if (!secondaryCard && trickster && (draft.cantrips.Contains(spellName) || draft.preparedSpells.Contains(spellName) || spellName == "Mage Hand")) description += "\n" + string.Format(translate("Магія шахрая: атака {0}; СК {1} (Інтелект)."), CharacterCreationRules.Signed(proficiency + CharacterCreationRules.Modifier(s.scores[3])), 8 + proficiency + CharacterCreationRules.Modifier(s.scores[3]));
                    int action = CharacterSpellCatalog.CastingOption(spell);
                    if (CharacterTricksterSpells.Active(owner) && draft.edition == RulesEdition.Rules2024 && spellName == "Mage Hand") action = 1;
                    if (CharacterBarbarianRules.Spells(owner).Contains(spellName)) action = 3;
                    if (CharacterWarlockRules.Modern(owner) && CharacterWarlockRules.Chain(owner) && spellName == "Find Familiar") action = 0;
                    int duration = CharacterSpellCatalog.DurationOption(spell);
                    if(CharacterDruidRules.Active(owner)&&CharacterDruidRules.Revised(owner)&&spellName=="Find Familiar"){action=0;duration=12;description+="\n"+translate(CharacterDruidRules.Text("companion"));}
                    if(CharacterDruidRules.Active(owner)&&!CharacterDruidRules.Revised(owner)&&spellName=="Alter Self")description+="\n"+translate(CharacterDruidRules.Text("moon.alter"));
                    if (CharacterMonkSubclassRules.Spells(owner).Contains(spellName) && spellName == "Sanctuary") duration = 12;
                    if (CharacterWarlockRules.Modern(owner) && CharacterWarlockRules.Chain(owner) && spellName == "Find Familiar") duration = 0;
                    int card = i % 15;
                    string pageName = i < 15 ? "spelBook" : "spelBook " + i / 15;
                    bindings.Text(c, "book.input." + card * 2, name, pageName);
                    bindings.Text(c, "book.input." + (card * 2 + 1), description, pageName);
                    bindings.Dropdown(c, "book.dropdown." + card * 3, action, pageName);
                    bindings.Dropdown(c, "book.dropdown." + (card * 3 + 1), duration, pageName);
                    bindings.Dropdown(c, "book.dropdown." + (card * 3 + 2), racialRebuke&&!classSource?2:spell.level, pageName);
                    bindings.Toggle(c, "book.prepared." + card, prepared, pageName);
                }
                var magic = c.GetSceneData("Spels");
                magic.SetString("StableField_v1_dnd.spels.inputfield.inputfield-legacy.7f8190968347", modifierText);
                magic.SetString("StableField_v1_dnd.spels.inputfield.inputfield-legacy.8753a5ac0f05", saveText);
                while (magic.inputData.Count <= 25) magic.inputData.Add("");
                magic.inputData[24] = modifierText; magic.inputData[25] = saveText;
            }
        }
    }
}
