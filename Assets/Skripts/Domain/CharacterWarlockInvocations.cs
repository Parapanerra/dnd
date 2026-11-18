using System;
using System.Linq;
using System.Collections.Generic;

namespace Taruck.CharacterCreation
{
    public static class CharacterWarlockInvocations
    {
        public sealed class Definition
        {
            public readonly string id,name,dependency,pact,spell;public readonly int minimum;public readonly bool modern;
            public Definition(string id,string name,int minimum,bool modern,string dependency,string pact,string spell){this.id=id;this.name=name;this.minimum=minimum;this.modern=modern;this.dependency=dependency;this.pact=pact;this.spell=spell;}
        }
        internal static Definition[] All=>CharacterWarlockInvocationData.All;
        public static string BaseId(string key)=>key.Split(':')[0];
        public static Definition Find(CharacterCreationDraft d,string key)=>All.FirstOrDefault(v=>v.modern==CharacterWarlockRules.Modern(d)&&v.id==BaseId(key));
        internal static bool Allowed(CharacterCreationDraft d,string key)
        {
            if(string.IsNullOrEmpty(key))return false;var item=Find(d,key);if(item==null||d.level<item.minimum)return false;
            if(item.dependency.Length>0&&!CharacterWarlockRules.Has(d,item.dependency))return false;
            if(item.pact.Length>0&&!(item.pact=="blade"?CharacterWarlockRules.Blade(d):item.pact=="chain"?CharacterWarlockRules.Chain(d):CharacterWarlockRules.Tome(d)))return false;
            string id=BaseId(key);
            if(new[]{"agonizing","spear","repelling"}.Contains(id))
            {
                if(!CharacterWarlockRules.Modern(d))return d.cantrips.Contains("Eldritch Blast");
                return key.Contains(":")&&EligibleCantrips(d,id).Contains(key.Substring(key.IndexOf(':')+1));
            }
            if(id=="lessons")
            {
                if(!key.StartsWith("lessons:",StringComparison.Ordinal))return false;string feat=key.Substring(8);
                return CharacterOriginFeatRules.Ids.Contains(feat)&&feat!=d.backgroundFeat?.id&&(!CharacterOriginFeatRules.HumanChoice(d)||feat!=d.humanFeat?.id);
            }
            return key==id;
        }
        internal static string[] EligibleCantrips(CharacterCreationDraft d,string invocation)=>d.cantrips.Concat(CharacterWarlockRules.Tome(d)?d.tomeCantrips:Enumerable.Empty<string>()).Distinct()
            .Where(name=>CharacterWarlockInvocationData.CantripEligible(d.edition==RulesEdition.Rules2024?2024:2014,name,invocation)).ToArray();
        public static string[] Options(CharacterCreationDraft d,int slot)
        {
            var candidates=new List<string>();foreach(var v in All.Where(v=>v.modern==CharacterWarlockRules.Modern(d)))
            {
                if(v.id=="lessons")candidates.AddRange(CharacterOriginFeatRules.Ids.Select(id=>"lessons:"+id));
                else if(v.modern&&new[]{"agonizing","spear","repelling"}.Contains(v.id))candidates.AddRange(EligibleCantrips(d,v.id).Select(name=>v.id+":"+name));else candidates.Add(v.id);
            }
            return candidates.Where(k=>Allowed(d,k)&&!d.warlockInvocations.Where((_,i)=>i!=slot).Contains(k)).ToArray();
        }
        public static string Name(CharacterCreationDraft d,string key,Func<string,string> translate)
        {
            var item=Find(d,key);if(item==null)return "";string result=translate(CharacterWarlockRules.Text("invname."+item.id));
            if(key.Contains(":")){string value=key.Substring(key.IndexOf(':')+1);result+=" — "+translate(item.id=="lessons"?CharacterOriginFeatRules.Name(value):value);}return result;
        }
        public static string Description(CharacterCreationDraft d,string key)
        {
            var v=Find(d,key);if(v==null)return "";string text=CharacterWarlockRules.Text("inv."+v.id+"."+(v.modern?"24":"14"));
            if(key.Contains(":"))text+="\n"+string.Format(CharacterWarlockRules.Text("chosen"),v.id=="lessons"?CharacterOriginFeatRules.Name(key.Substring(8)):key.Substring(key.IndexOf(':')+1));return text;
        }
        public static string[] FeatureSpells(CharacterCreationDraft d)=>!CharacterWarlockRules.Active(d)?Array.Empty<string>():d.warlockInvocations.Select(k=>Find(d,k)?.spell??"").Where(s=>s.Length>0)
            .Concat(CharacterWarlockRules.Chain(d)?new[]{"Find Familiar"}:Array.Empty<string>()).Concat(d.warlockArcana).Where(s=>s.Length>0).Distinct().ToArray();
        public static readonly string[] Limited14={"whispers","word","chaos","mire","sculptor","omen","fates"};
    }
}
