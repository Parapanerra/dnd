using System;
using System.Linq;
using System.Collections.Generic;
namespace Taruck.CharacterCreation
{
    internal static class CreationWarlockResources
    {
        internal static string SpellNote(CharacterCreationDraft d,string name,Func<string,string> t)
        {
            if(!CharacterWarlockRules.Active(d))return "";
            var lines=new List<string>();
            if(d.warlockArcana.Contains(name))lines.Add(t(CharacterWarlockRules.Text("arcanum.cast")));
            foreach(string key in d.warlockInvocations.Where(k=>k.Length>0))
            {
                var invocation=CharacterWarlockInvocations.Find(d,key);
                bool grant=invocation!=null&&invocation.spell==name;
                bool enhanced=CharacterWarlockRules.Modern(d)?key.EndsWith(":"+name,StringComparison.Ordinal)&&new[]{"agonizing","spear","repelling"}.Contains(CharacterWarlockInvocations.BaseId(key)):name=="Eldritch Blast"&&new[]{"agonizing","spear","repelling"}.Contains(key);
                if(grant||enhanced)lines.Add(CharacterCreationPresentation.FeatureLines(CharacterWarlockInvocations.Description(d,key),t));
            }
            if(CharacterWarlockRules.Chain(d)&&name=="Find Familiar"&&!CharacterWarlockRules.Modern(d))lines.Add(t(CharacterWarlockRules.Text("pact.chain")));
            if(CharacterWarlockRules.Tome(d)&&!CharacterWarlockRules.Modern(d)&&d.tomeRituals.Contains(name))lines.Add(t(CharacterWarlockRules.Text("ritual.only")));
            if(CharacterWarlockRules.Modern(d)&&d.level>=9&&name=="Contact Other Plane")lines.Add(t(CharacterWarlockRules.Text("contact")));
            return string.Join("\n",lines.Distinct());
        }
        internal static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,CreatedCharacterStats stats,Func<string,string> t)
        {
            bool modern=CharacterWarlockRules.Modern(d);int cha=Math.Max(1,CharacterCreationRules.Modifier(stats.scores[5]));
            string Label(string key)=>t(CharacterWarlockRules.Text(key));
            void Pool(string key,int count=1,bool shortRest=false){int part=1;while(count>0){int take=Math.Min(8,count);CreationMagicResources.Add(c,b,Label(key)+(part>1?" ("+part+")":""),take,shortRest);count-=take;part++;}}
            if(modern&&d.level>=2)Pool("pool.cunning");if(!modern&&d.level>=20)Pool("pool.master");if(modern&&d.level>=9)Pool("pool.contact");
            for(int i=0;i<d.warlockArcana.Count;i++)CreationMagicResources.Add(c,b,string.Format(Label("pool.arcanum"),i+6),1,false);
            if(CharacterWarlockRules.Is(d,"archfey"))
            {
                if(modern){Pool("pool.steps",cha);if(d.level>=10)Pool("pool.defenses");}
                else{CreationMagicResources.Add(c,b,t("Присутність фей"),1,true);if(d.level>=6)Pool("pool.escape",1,true);if(d.level>=14)Pool("pool.delirium",1,true);}
            }
            if(CharacterWarlockRules.Is(d,"fiend")){if(d.level>=6)Pool("pool.luck",modern?cha:1,!modern);if(d.level>=14)Pool("pool.hell");}
            if(CharacterWarlockRules.Is(d,"greatoldone")&&d.level>=6)Pool(modern?"pool.combatant":"pool.ward",1,true);
            if(CharacterWarlockRules.Is(d,"celestial")){Pool("pool.light",d.level+1);if(d.level>=14)Pool("pool.vengeance");}
            if(!d.IsSecondaryTrack&&CharacterWarlockRules.Blade(d)&&!modern&&CharacterWarlockRules.Has(d,"lifedrinker"))b.Text(c,"input.68",CharacterWarlockRules.PactDamage[d.pactWeapon]+CharacterCreationRules.Signed(CharacterWarlockRules.PactModifier(d,stats.scores))+" + "+cha+" ("+t("Некротична")+")");
            var ordered=d.warlockInvocations.OrderByDescending(k=>CharacterWarlockInvocations.Limited14.Contains(k)||k=="depths"||k=="protectors").ToArray();
            b.Dropdown(c,"magic.invocation.count",ordered.Length);
            for(int row=0;row<10;row++)
            {
                string key=row<ordered.Length?ordered[row]:"";
                b.Text(c,"magic.invocation."+row+".name",key.Length==0?"":CharacterWarlockInvocations.Name(d,key,t));
                b.Toggle(c,"rest.magic.invocation."+row,false);
                if(key.Length>0&&(!modern&&CharacterWarlockInvocations.Limited14.Contains(key)||key=="depths"||key=="protectors"))CreationResourceRecovery.Register(c,b,"rest.magic.invocation."+row,false);
            }
            // Effects live next to their native invocation list, not duplicated in the main sheet notes.
        }
    }
}
