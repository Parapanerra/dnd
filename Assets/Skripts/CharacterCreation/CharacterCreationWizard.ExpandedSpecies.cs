using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private void ExpandedOriginChoices()
        {
            if(!CharacterExpandedSpeciesRules.Active(draft))return;
            string Text(string key)=>CharacterExpandedSpeciesRules.Text(key);
            Note(LF(Text("ui.source"),CharacterExpandedSpeciesRules.Source(draft)));
            if(CharacterFtdDragonbornRules.Active(draft))
            {
                var families=CharacterFtdDragonbornRules.Families;
                DropdownKey(CharacterFtdDragonbornRules.Text("ui.family"),families,families.Select(key=>CharacterFtdDragonbornRules.Text("family."+key)).ToArray(),draft.speciesLineage,value=>{draft.speciesLineage=value;draft.speciesAncestor=-1;},300);
                if(families.Contains(draft.speciesLineage))DropdownInt(CharacterFtdDragonbornRules.Text("ui.ancestor"),Enumerable.Range(0,5).ToArray(),draft.speciesAncestor,value=>draft.speciesAncestor=value,value=>CharacterFtdDragonbornRules.Ancestor(draft,value),300);
            }
            if(draft.species=="simic")
            {
                DropdownText(Text("ui.simiclanguage"),new[]{"Ельфійська","Ведалкенська"},draft.speciesLanguage,value=>draft.speciesLanguage=value);
                if(draft.edition==RulesEdition.Rules2014)DropdownInt(Text("ui.simicability"),new[]{0,1,3,4,5},draft.expandedIncreases[0],value=>draft.expandedIncreases[0]=value,value=>CharacterCreationRules.AbilityNames[value],230);
                for(int i=0;i<(draft.TotalLevel>=5?2:1);i++){int slot=i;var options=CharacterExpandedSpeciesRules.SimicOptions(draft,slot);DropdownKey(LF(Text("ui.simic"),i+1),options,options.Select(key=>Text("simic."+key)).ToArray(),draft.simicEnhancements[slot],value=>draft.simicEnhancements[slot]=value,300);}
            }
            if(draft.species=="shifter")DropdownKey(Text("ui.shifter"),CharacterExpandedSpeciesRules.ShifterLineages,CharacterExpandedSpeciesRules.ShifterLineages.Select(key=>Text("shifter."+key)).ToArray(),draft.speciesLineage,value=>draft.speciesLineage=value,300);
            if(draft.species=="genasi")DropdownKey(Text("ui.genasi"),CharacterExpandedSpeciesRules.GenasiLineages,CharacterExpandedSpeciesRules.GenasiLineages.Select(key=>Text("lineage."+key)).ToArray(),draft.speciesLineage,value=>draft.speciesLineage=value,300);
            if(draft.edition==RulesEdition.Rules2024&&CharacterExpandedSpeciesRules.Source(draft)!="EFA")Note(Text("ui.compatible"));
            if(CharacterExpandedSpeciesRules.Flexible(draft))
            {
                ui.Text(content,Text("ui.increases"),25,CreationWizardUi.Gold,42);
                var row=ui.Row("SpeciesAbilityMethod",content,58);
                ui.Button(row,"+2 та +1",()=>{draft.expandedBonusMode=0;Draw(false);},draft.expandedBonusMode==0);
                ui.Button(row,"+1 до трьох",()=>{draft.expandedBonusMode=1;Draw(false);},draft.expandedBonusMode==1);
                int count=draft.expandedBonusMode==0?2:draft.expandedBonusMode==1?3:0;
                for(int i=0;i<count;i++)
                {
                    int slot=i;
                    DropdownInt(LF(Text("ui.increase"),draft.expandedBonusMode==0&&i==0?2:1),Enumerable.Range(0,6).Where(value=>!draft.expandedIncreases.Take(count).Where((v,index)=>index!=slot).Contains(value)).ToArray(),draft.expandedIncreases[slot],value=>draft.expandedIncreases[slot]=value,value=>CharacterCreationRules.AbilityNames[value],230);
                }
            }
            if(draft.species=="aasimarlegacy"&&draft.TotalLevel>=3)DropdownKey(Text("ui.revelation"),CharacterExpandedSpeciesRules.AasimarRevelations,CharacterExpandedSpeciesRules.AasimarRevelations.Select(key=>Text("revelation."+key)).ToArray(),draft.speciesLineage,value=>draft.speciesLineage=value,300);
            if(draft.species=="khoravar")
            {
                var spells=new[]{"cleric","druid","wizard"}.SelectMany(cls=>CharacterSpellCatalog.Available(draft.edition,cls,0)).Select(spell=>spell.name).Distinct().ToArray();
                DropdownKey(Text("ui.feygift"),spells,spells.Select(L).ToArray(),string.IsNullOrEmpty(draft.expandedCantrip)?"Friends":draft.expandedCantrip,value=>draft.expandedCantrip=value,300);
            }
            if(draft.species=="kobold")
            {
                var gifts=new[]{"craft","defiance","magic"};
                DropdownKey(Text("ui.gift"),gifts,gifts.Select(key=>Text("gift."+key)).ToArray(),draft.expandedGift,value=>{draft.expandedGift=value;draft.expandedCantrip="";draft.expandedMagicAbility=-1;},300);
                if(draft.expandedGift=="magic")
                {
                    var choices=CharacterSpellCatalog.Available(draft.edition,"sorcerer",0).Where(spell=>!draft.cantrips.Contains(spell.name)&&!draft.tomeCantrips.Contains(spell.name)).Select(spell=>spell.name).ToArray();
                    DropdownKey(Text("ui.koboldcantrip"),choices,choices.Select(L).ToArray(),draft.expandedCantrip,value=>draft.expandedCantrip=value,300);
                }
            }
            if(draft.species=="astralelf")
            {
                string[] spells=new[]{"Dancing Lights","Light","Sacred Flame"}.Where(name=>!draft.cantrips.Contains(name)&&!draft.tomeCantrips.Contains(name)).ToArray();
                DropdownKey(Text("ui.fire"),spells,spells.Select(L).ToArray(),draft.expandedCantrip,value=>draft.expandedCantrip=value,300);
                DropdownInt(Text("ui.magic"),new[]{3,4,5},draft.expandedMagicAbility,value=>draft.expandedMagicAbility=value,value=>CharacterCreationRules.AbilityNames[value],230);
            }
            else if(CharacterExpandedSpeciesRules.UsesMagic(draft))DropdownInt(Text("ui.racialmagic"),new[]{3,4,5},draft.expandedMagicAbility,value=>draft.expandedMagicAbility=value,value=>CharacterCreationRules.AbilityNames[value],230);
            if(draft.species=="eladrin")
            {
                DropdownKey(Text("ui.season"),CharacterExpandedSpeciesRules.EladrinSeasons,CharacterExpandedSpeciesRules.EladrinSeasons.Select(key=>Text("season."+key)).ToArray(),draft.speciesLineage,value=>draft.speciesLineage=value,230);
                DropdownInt(Text("ui.stepability"),new[]{3,4,5},draft.expandedMagicAbility,value=>draft.expandedMagicAbility=value,value=>CharacterCreationRules.AbilityNames[value],230);
            }
            if(draft.species=="kender")DropdownInt(Text("ui.tauntability"),new[]{3,4,5},draft.expandedMagicAbility,value=>draft.expandedMagicAbility=value,value=>CharacterCreationRules.AbilityNames[value],230);
            Note(CharacterCreationPresentation.FeatureLines(CharacterExpandedSpeciesRules.Features(draft),L));
        }
        private void ExpandedSkillChoices()
        {
            if(!CharacterExpandedSpeciesRules.Active(draft))return;
            if(draft.species=="khoravar")DropdownKey(CharacterExpandedSpeciesRules.Text("ui.versatility"),new[]{"skill","tool"},new[]{CharacterExpandedSpeciesRules.Text("ui.oneskill"),CharacterExpandedSpeciesRules.Text("ui.onetool")},draft.expandedGift,value=>{draft.expandedGift=value;draft.expandedSkills=new[]{-1,-1};draft.expandedTools=new[]{"",""};},300);
            for(int i=0;i<CharacterExpandedSpeciesRules.SkillCount(draft);i++)
            {
                int slot=i;
                DropdownInt(LF(CharacterExpandedSpeciesRules.Text("ui.skill"),i+1),CharacterExpandedSpeciesRules.SkillOptions(draft,slot),draft.expandedSkills[slot],value=>draft.expandedSkills[slot]=value,value=>CharacterCreationRules.SkillNames[value],300);
            }
            if(CharacterExpandedSpeciesRules.ToolCount(draft)>0)for(int i=0;i<CharacterExpandedSpeciesRules.ToolCount(draft);i++)
            {
                int slot=i;DropdownText(LF(CharacterExpandedSpeciesRules.Text("ui.tool"),i+1),CharacterExpandedSpeciesRules.ToolOptions(draft,slot),draft.expandedTools[slot],value=>draft.expandedTools[slot]=value);
            }
            for(int i=0;i<CharacterExpandedSpeciesRules.TranceCount(draft);i++)
            {
                int slot=i;var keys=CharacterExpandedSpeciesRules.TranceOptions(draft,slot);
                DropdownKey(LF(CharacterExpandedSpeciesRules.Text(draft.species=="githyanki"?"ui.astralknowledgechoice":"ui.trancechoice"),i+1),keys,keys.Select(key=>L(key.Substring(key.IndexOf(':')+1))).ToArray(),CharacterExpandedSpeciesRules.Trance(draft,slot),value=>{if(slot==0)draft.expandedTranceProficiency=value;else draft.expandedSecondTranceProficiency=value;},300);
            }
            if(draft.species=="satyr")DropdownText(CharacterExpandedSpeciesRules.Text("ui.instrument"),CharacterExpandedSpeciesRules.InstrumentOptions(draft),draft.expandedInstrument,value=>draft.expandedInstrument=value);
        }
    }
}
