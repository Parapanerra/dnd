using System;
using System.Linq;
using System.Collections.Generic;
namespace Taruck.CharacterCreation
{
    internal static class CreationDruidForms
    {
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft d,CreatedCharacterStats stats) => CharacterDruidFormChoices.Options(d)
            .Where(f=>!CharacterDruidRules.Revised(d)||d.druidKnownForms.Contains(f.id))
            .Select(f=>new ReadyFormTemplate(f.id,f.name,(c,b,slot,t)=>Write(c,b,d,stats,f,slot,t))).Concat(Familiars(d)).ToArray();
        private static ReadyFormTemplate[] Familiars(CharacterCreationDraft d) => !CharacterDruidRules.Active(d)||!CharacterDruidRules.Revised(d)||d.level<2 ? Array.Empty<ReadyFormTemplate>() : CharacterDruidFormChoices.All.Where(f=>f.edition==2024&&new[]{"Cat","Rat","Spider"}.Contains(f.sourceName)).Select(f=>new ReadyFormTemplate("druid.companion."+f.id,CharacterDruidRules.Text("familiar."+f.sourceName.ToLowerInvariant()),(c,b,slot,t)=>{
            string scene=WildShapeFormList.SceneNameForSlot(slot);void Field(int i,string value)=>b.Text(c,"form.input."+i,value,scene);
            Field(1,t(CharacterDruidRules.Text("familiar."+f.sourceName.ToLowerInvariant())));Field(8,f.ac.ToString());Field(7,f.speed.ToString());Field(4,f.passive.ToString());Field(6,CharacterCreationRules.Signed(CharacterCreationRules.Modifier(f.scores[1])));
            for(int i=0;i<6;i++){Field(9+2*i,CharacterCreationRules.Signed(CharacterCreationRules.Modifier(f.scores[i])));Field(10+2*i,f.scores[i].ToString());}
            Field(2,t(CharacterDruidRules.Text("form.familiar"))+"\n\n"+string.Join("\n",(f.notes??"").Split('\n').Where(line=>line.Length>0&&!line.StartsWith("Укус:",StringComparison.Ordinal)).Select(t)));Field(41,t(CharacterDruidRules.Text("companion")));
            var state=c.GetSceneData(scene);state.SetInt(b.FormHealthPrefix+"maxHealth",f.hp);state.SetInt(b.FormHealthPrefix+"currentHealth",f.hp);state.SetInt(b.FormHealthPrefix+"maxTemporaryHealth",0);state.SetInt(b.FormHealthPrefix+"currentTemporaryHealth",0);
        })).ToArray();
        private static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,CreatedCharacterStats stats,DruidBeast f,int slot,Func<string,string> t)
        {
            string scene=WildShapeFormList.SceneNameForSlot(slot);void Field(int i,string value)=>b.Text(c,"form.input."+i,value,scene);
            string T(string key)=>t(CharacterDruidRules.Text(key));string Signed(int n)=>CharacterCreationRules.Signed(n);
            bool modern=CharacterDruidRules.Revised(d),moon=CharacterDruidRules.Is(d,"moon");
            int wisdom=CharacterCreationRules.Modifier(stats.scores[4]),pb=CharacterLegacySpeciesRules.Proficiency(d);
            int[] scores=f.scores.Take(3).Concat(stats.scores.Skip(3)).ToArray();
            Field(1,t(f.name));Field(0,Signed(wisdom));Field(44,(8+pb+wisdom).ToString());Field(3,Signed(pb));Field(6,Signed(CharacterCreationRules.Modifier(scores[1])));
            Field(7,(f.speed>0?f.speed:f.fly>0?f.fly:f.swim).ToString());Field(8,(modern&&moon?Math.Max(f.ac,13+wisdom):f.ac).ToString());
            for(int i=0;i<6;i++){Field(9+2*i,Signed(CharacterCreationRules.Modifier(scores[i])));Field(10+2*i,scores[i].ToString());}
            int Save(int i)=>Math.Max(CharacterCreationRules.Modifier(scores[i])+(i==3||i==4?pb:0),f.saves.Where(x=>x.index==i).Select(x=>x.bonus).DefaultIfEmpty(-99).Max())+(modern&&moon&&d.level>=6&&i==2?wisdom:0);
            int Skill(int i)=>Math.Max(CharacterCreationRules.Modifier(scores[CharacterCreationRules.SkillAbilities[i]])+(stats.expertise!=null&&stats.expertise.Contains(i)?2*pb:stats.skills.Contains(i)?pb:0)+(modern&&d.primalOrder=="magician"&&(i==2||i==10)?Math.Max(1,wisdom):0),f.skills.Where(x=>x.index==i).Select(x=>x.bonus).DefaultIfEmpty(-99).Max());
            Field(4,(10+Skill(11)).ToString());
            var traits=new List<string>{T("form.mental"),string.Format(T("form.movement"),f.speed,f.fly,f.swim,f.climb,f.burrow)};
            traits.AddRange((f.notes??"").Split('\n').Where(line=>line.Length>0).Select(t));
            traits.Add(T("form.saves")+": "+string.Join(", ",Enumerable.Range(0,6).Select(i=>t(CharacterCreationRules.AbilityNames[i])+" "+Signed(Save(i)))));
            int[] skills=stats.skills.Concat(f.skills.Select(x=>x.index)).Distinct().ToArray();
            if(skills.Length>0)traits.Add(T("form.skills")+": "+string.Join(", ",skills.Select(i=>t(CharacterCreationRules.SkillNames[i])+" "+Signed(Skill(i)))));
            Field(2,string.Join("\n\n",traits));
            for(int i=0;i<Math.Min(6,f.attacks.Length);i++){int field=i<3?21+3*i:31+3*(i-3);var attack=f.attacks[i];Field(field,Signed(attack.bonus));Field(field+1,t(attack.name));Field(field+2,attack.damage);}
            var combat=new List<string>();
            if(moon){if(modern){combat.Add(T("moon.spells"));if(d.level>=6)combat.Add(T("moon.improved"));if(d.level>=14)combat.Add(T("moon.lunar"));}else{combat.Add(T("moon.combat"));if(d.level>=6)combat.Add(T("moon.magical"));if(f.elemental)combat.Add(T("form.elemental"));}}
            if(modern&&d.level>=7)combat.Add(string.Format(T(CharacterDruidRules.Potent(d)?"potent":"primal"),d.level>=15?"2d8":"1d8"));
            Field(30,string.Join("\n\n",combat));
            Field(41,T(modern?"wild24":"wild14").Replace("{0}",Math.Floor(d.level/2d).ToString())+"\n\n"+T(modern?"form.ownerhp":"form.beasthp"));
            Field(42,T("form.growth")+(modern?"\n\n"+T("form.temp"):""));
            Field(43,T("form.snapshot")+"\n\n"+T(modern?"beastspells24":"beastspells14"));
            var state=c.GetSceneData(scene);int hp=modern?stats.hitPoints:f.hp,temp=modern?d.level*(moon?3:1):0;
            state.SetInt(b.FormHealthPrefix+"maxHealth",hp);state.SetInt(b.FormHealthPrefix+"currentHealth",hp);state.SetInt(b.FormHealthPrefix+"maxTemporaryHealth",temp);state.SetInt(b.FormHealthPrefix+"currentTemporaryHealth",temp);
            if(modern){state.SetInt("DruidSharedHealth_v1",1);state.SetString("DruidSharedHealthPrefix_v1",b.HealthPrefix);}
        }
    }
}
