using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationBardCompanions
    {
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft d, CreatedCharacterStats stats)
        {
            if((!CharacterWarlockRules.Active(d)&&!CharacterBardRules.Active(d)&&!CharacterDruidRules.Active(d))||d.edition!=RulesEdition.Rules2024)return Array.Empty<ReadyFormTemplate>();
            var spells=d.preparedSpells.Concat(CharacterDruidRules.AutomaticSpells(d)).Concat(d.bardSecrets).Concat(d.warlockArcana).ToArray();var result=new List<ReadyFormTemplate>();
            foreach(bool insect in new[]{true,false})if(spells.Contains(insect?"Giant Insect":"Summon Fiend"))for(int i=0;i<3;i++) {
                int variant=i;bool kind=insect;
                result.Add(new ReadyFormTemplate((insect?"summon.insect.":"summon.fiend.")+i,CharacterBardText.Get((insect?"form.insect.":"form.fiend.")+i),
                    (c,b,slot,t)=>Write(c,b,slot,d,kind,variant,t)));
            }
            return result.ToArray();
        }
        private static void Write(CharacterData c,CreationSheetBindings b,int slot,CharacterCreationDraft d,bool insect,int variant,Func<string,string> t)
        {
            string scene=WildShapeFormList.SceneNameForSlot(slot);void Text(int n,string value)=>b.Text(c,"form.input."+n,value,scene);
            int pb=CharacterLegacySpeciesRules.Proficiency(d),modifier=CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[CharacterDruidRules.Active(d)?4:5]),dc=8+pb+modifier;
            int circle=insect?4:6,hp=insect?30:new[]{50,40,60}[variant],ac=insect?15:18;
            int[] scores=insect?new[]{17,13,15,4,14,3}:new[]{13,16,15,10,10,16};
            string name=t(CharacterBardText.Get((insect?"form.insect.":"form.fiend.")+variant));Text(1,name);c.SetSharedString("WildShapeFormName_"+slot,name);
            Text(0,CharacterCreationRules.Signed(modifier));Text(3,CharacterCreationRules.Signed(pb));Text(44,dc.ToString());Text(4,insect?"12":"10");
            Text(6,CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[1])));Text(7,"40");Text(8,ac.ToString());
            for(int i=0;i<6;i++) { Text(9+2*i,CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i])));Text(10+2*i,scores[i].ToString()); }
            Text(21,CharacterCreationRules.Signed(pb+modifier));
            Text(22,t(CharacterBardText.Get(insect?"form.jab":variant==0?"form.bite":variant==1?"form.fire":"form.claws")));
            string damage=insect?"1d6+7 + 1d4":variant==0?"1d12+9":variant==1?"2d6+9":"1d8+9";Text(23,damage);
            if(insect&&variant==1) { Text(24,CharacterCreationRules.Signed(pb+modifier));Text(25,t(CharacterBardText.Get("form.web")));Text(26,"1d10+7"); }
            Text(2,t(CharacterBardText.Get(insect?"form.insect.common":"form.fiend.common"))+"\n\n"+t(CharacterBardText.Get((insect?"form.insect.traits.":"form.fiend.traits.")+variant)));
            Text(30,t(CharacterBardText.Get(insect?"form.insect.attack":"form.fiend.attack."+variant)));
            Text(41, ReadyFormProgressionNotes.SlotGrowth(circle, hp, ac, insect ? 10 : 15, damage, t));
            Text(42,t(CharacterBardText.Get(insect&&variant==1?"form.insect.growth.web":!insect&&variant==0?"form.fiend.growth.demon":"form.growth")));
            Text(43,t(CharacterDruidRules.Active(d)?CharacterDruidRules.Text("form.snapshot"):CharacterSorcererText.Get("form.charisma")));
            var state=c.GetSceneData(scene);state.SetInt(b.FormHealthPrefix+"maxHealth",hp);state.SetInt(b.FormHealthPrefix+"currentHealth",hp);state.SetInt(b.FormHealthPrefix+"maxTemporaryHealth",0);state.SetInt(b.FormHealthPrefix+"currentTemporaryHealth",0);
        }
    }
}
