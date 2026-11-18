using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CharacterCreationChoices
    {
        internal static void SetEdition(CharacterCreationDraft draft, RulesEdition edition)
        {
            draft.edition = edition;
            if (CharacterBackgroundRules.Find(draft) == null) SetBackground(draft, "");
            if (!CharacterSpeciesRules.Available(edition).Contains(draft.species)) SetSpecies(draft, "");
            if (CharacterCreationRules.MaxSupportedLevel(draft) == 0 && !string.IsNullOrEmpty(draft.characterClass)) SetClass(draft, "");
            CharacterCreationRules.RepairDependentChoices(draft);
        }
        internal static void SetClass(CharacterCreationDraft draft, string selectedClass)
        {
            if (draft.characterClass == selectedClass) return;
            draft.characterClass = selectedClass;
            draft.level = -1;
            draft.subclass = "";
            draft.barbarianSpirit = draft.barbarianAspect = draft.barbarianAttunement = "";
            draft.advancementModes = Enumerable.Repeat(-1, 6).ToArray();
            draft.advancementFirst = Enumerable.Repeat(-1, 6).ToArray();
            draft.advancementSecond = Enumerable.Repeat(-1, 6).ToArray();
            draft.epicBoon = ""; draft.epicAbility = -1;
            draft.hitPointChoices = new int[19];
            draft.classSkills.Clear();
            if (draft.expertise == null) draft.expertise = new System.Collections.Generic.List<int>();
            else draft.expertise.Clear();
            if (draft.weaponMasteries == null) draft.weaponMasteries = new System.Collections.Generic.List<int>();
            else draft.weaponMasteries.Clear();
            if (draft.preparedSpells == null) draft.preparedSpells = new System.Collections.Generic.List<string>();
            else draft.preparedSpells.Clear();
            draft.cantrips.Clear(); draft.bardInstruments.Clear(); draft.bardWeapon = ""; draft.bardPack = -1; draft.bardCarriedInstrument = "";
            draft.primalOrder = ""; draft.druidFirstItem = ""; draft.druidWeapon = "";
            draft.spellbookSpells.Clear(); draft.wizardWeapon = ""; draft.wizardFocus = ""; draft.wizardPack = -1;
            draft.primalKnowledgeSkill = -1;
            draft.rogueLanguage = "";
            draft.fighterManeuvers.Clear(); draft.fighterTool = ""; draft.fighterSkill = -1; draft.fighterSecondStyle = -1;
            draft.rangerEnemy = ""; draft.rangerHumanoids = ""; draft.rangerTerrain = ""; draft.rangerLanguage = "";
            draft.fightingStyle = (FighterStyle)(-1);
            draft.equipment = -1;
            draft.artisanTool = "";
            draft.monkTool = "";
            draft.monkPack = -1;
            draft.monkWeapon = "";
            CharacterCreationRules.RepairDependentChoices(draft);
        }
        internal static void SetSpecies(CharacterCreationDraft draft, string selectedSpecies)
        {
            if (draft.species == selectedSpecies) return;
            draft.species = selectedSpecies;
            draft.speciesLineage = "";
            draft.originSkillReplacement = -1; draft.secondOriginSkillReplacement=-1;
            draft.speciesSkill = -1; draft.speciesMagicAbility = -1; draft.speciesCantrip = ""; draft.speciesLanguage = "";
            draft.originToolReplacement = "";
            draft.speciesAncestor = -1; draft.speciesSize = ""; draft.speciesTool = ""; draft.speciesIncreases = new[] { -1, -1 }; draft.speciesSkills = new[] { -1, -1 };
            draft.simicEnhancements=new[]{"",""};draft.expandedUnarmored=false;draft.expandedTools=new[]{"",""};draft.expandedGift="";draft.expandedInstrument="";draft.expandedBonusMode=-1;draft.expandedIncreases=new[]{-1,-1,-1};draft.expandedSkills=new[]{-1,-1};draft.expandedSecondTranceProficiency=draft.expandedTranceProficiency=draft.expandedCantrip="";draft.expandedMagicAbility=-1;
            draft.humanSkill = -1;
            draft.artisanTool = "";
            CharacterCreationRules.RepairDependentChoices(draft);
        }
        internal static void SetBackground(CharacterCreationDraft draft, string selectedBackground)
        {
            if (draft.background == selectedBackground) return;
            draft.background = selectedBackground;
            draft.backgroundTool = ""; draft.backgroundItem = ""; draft.backgroundLanguages = new[] { "", "" }; draft.backgroundToolReplacements = new[] { "", "" };
            draft.originSkillReplacement = -1; draft.secondOriginSkillReplacement=-1;
            draft.backgroundEquipmentChoice = -1;
            draft.gamingSet = "";
            draft.bonusMode = -1;
            draft.increaseTwo = -1;
            draft.increaseOne = -1;
            CharacterCreationRules.RepairDependentChoices(draft);
        }

        internal static void RepairCatalogChoices(CharacterCreationDraft draft)
        {
            CharacterClericSpellChoices.Repair(draft);
            CharacterDruidFormChoices.Repair(draft);
            if (CharacterTricksterSpells.ThirdCaster(draft))
            {
                var availableNames = CharacterTricksterSpells.Available(draft, 1).Select(spell => spell.name).ToArray();
                for (int i = 0; i < draft.preparedSpells.Count; i++) if (!availableNames.Contains(draft.preparedSpells[i])) draft.preparedSpells[i] = "";
            }
            if (CharacterBardRules.Active(draft) || CharacterPaladinRules.Active(draft) || CharacterRangerRules.Active(draft) || CharacterSorcererRules.Active(draft))
            {
                var availableNames = CharacterSpellCatalog.AvailableFor(draft, 1).Select(spell => spell.name).ToArray();
                for (int i = 0; i < draft.preparedSpells.Count; i++) if (!availableNames.Contains(draft.preparedSpells[i])) draft.preparedSpells[i] = "";
                if(CharacterBardRules.Active(draft))for(int i=0;i<draft.bardSecrets.Count;i++)
                    if(!CharacterSpellCatalog.BardSecrets(draft,i).Any(spell=>spell.name==draft.bardSecrets[i]))draft.bardSecrets[i]="";
            }
        }
    }
}
