using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterDruidRules
    {
        public static readonly string[] Circles={"land","moon","sea","stars"};
        public static readonly string[] ClassicLands={"arctic","coast","desert","forest","grassland","mountain","swamp","underdark"};
        public static readonly string[] ModernLands={"arid","polar","temperate","tropical"};
        public static bool Active(CharacterCreationDraft d)=>d.characterClass=="druid";
        public static bool Revised(CharacterCreationDraft d)=>d.edition==RulesEdition.Rules2024;
        public static int SubclassLevel(CharacterCreationDraft d)=>Revised(d)?3:2;
        public static string[] Options(CharacterCreationDraft d)=>Revised(d)?Circles:Circles.Take(2).ToArray();
        public static bool Is(CharacterCreationDraft d,string id)=>Active(d)&&d.level>=SubclassLevel(d)&&d.subclass==id&&Options(d).Contains(id);
        public static string[] Lands(CharacterCreationDraft d)=>Revised(d)?ModernLands:ClassicLands;
        public static string Text(string key)=>CharacterDruidText.Get(key);
        internal static int Cantrips(CharacterCreationDraft d)=>(d.level>=10?4:d.level>=4?3:2)+(Revised(d)&&d.primalOrder=="magician"?1:0)+(!Revised(d)&&Is(d,"land")?1:0);
        internal static int SpellCount(CharacterCreationDraft d)=>Revised(d)?new[]{4,5,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22}[Math.Clamp(d.level,1,20)-1]:Math.Max(1,d.level+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[4]));
        internal static int Slots(CharacterCreationDraft d,int circle)=>Active(d)?CharacterFullCasterSlots.Count(d.level,circle):0;
        public static int MaximumCircle(CharacterCreationDraft d)=>Math.Min(9,(d.level+1)/2);
        public static int WildUses(CharacterCreationDraft d)=>d.level<2?0:Revised(d)?d.level>=17?4:d.level>=6?3:2:d.level>=20?-1:2;
        public static int KnownForms(CharacterCreationDraft d)=>!Revised(d)||d.level<2?0:d.level>=8?8:d.level>=4?6:4;
        public static double MaxCR(CharacterCreationDraft d)=>Is(d,"moon")?Revised(d)||d.level>=6?Math.Floor(d.level/3d):1:d.level>=8?1:d.level>=4?.5:.25;
        public static bool Flying(CharacterCreationDraft d)=>d.level>=8;
        public static bool Swimming(CharacterCreationDraft d)=>Revised(d)||d.level>=4;
        public static bool Potent(CharacterCreationDraft d)=>Active(d)&&Revised(d)&&d.level>=7&&d.druidFury=="potent";
        public static string[] FeatureSpells(CharacterCreationDraft d)=>!Active(d)?Array.Empty<string>():
            (Revised(d)&&d.level>=2?new[]{"Find Familiar"}:Array.Empty<string>()).Concat(!Revised(d)&&Is(d,"moon")&&d.level>=14?new[]{"Alter Self"}:Array.Empty<string>()).ToArray();
        public static string[] AutomaticSpells(CharacterCreationDraft d)=>!Active(d)?Array.Empty<string>():CircleSpells(d)
            .Concat(Revised(d)?new[]{"Speak with Animals"}:Array.Empty<string>()).Concat(Is(d,"stars")?new[]{"Guidance","Guiding Bolt"}:Array.Empty<string>()).Distinct().ToArray();
        internal static void Repair(CharacterCreationDraft d)
        {
            d.druidKnownForms??=new List<string>();
            if(!Active(d)){d.druidLand=d.druidFury="";d.druidKnownForms.Clear();return;}
            if(d.level<SubclassLevel(d)||!Options(d).Contains(d.subclass))d.subclass="";
            if(!Is(d,"land")||!Lands(d).Contains(d.druidLand))d.druidLand="";
            if(!Revised(d)||d.level<7)d.druidFury="";
            if(d.druidKnownForms.Count>KnownForms(d))d.druidKnownForms.RemoveRange(KnownForms(d),d.druidKnownForms.Count-KnownForms(d));
            while(d.druidKnownForms.Count<KnownForms(d))d.druidKnownForms.Add("");
            var auto=AutomaticSpells(d);d.cantrips.RemoveAll(auto.Contains);d.preparedSpells.RemoveAll(auto.Contains);
        }
        public static string ChoiceError(CharacterCreationDraft d,int page)
        {
            if(!Active(d))return null;
            if(page==0){
                if(d.level>=SubclassLevel(d)&&!Options(d).Contains(d.subclass))return Text("error.circle");
                if(Is(d,"land")&&d.level>=3&&!Lands(d).Contains(d.druidLand))return Text("error.land");
                if(Revised(d)&&d.level>=7&&!new[]{"potent","primal"}.Contains(d.druidFury))return Text("error.fury");
            }
            return null;
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var lines=new List<string>();void Add(string id,params object[] args)=>lines.Add(args.Length==0?Text(id):string.Format(Text(id),args));
            Add(Revised(d)?"druidic24":"druidic14");
            if(Revised(d)&&new[]{"magician","warden"}.Contains(d.primalOrder))Add(d.primalOrder=="magician"?"order.magician":"order.warden");
            if(d.level>=2){Add(Revised(d)?"wild24":"wild14",Math.Floor(d.level/2d));Add("wild.limits",MaxCR(d).ToString(System.Globalization.CultureInfo.InvariantCulture),Flying(d)?Text("yes"):Text("no"),Swimming(d)?Text("yes"):Text("no"));if(Revised(d))Add("companion");}
            if(Revised(d)&&d.level>=5)Add("resurgence");
            if(Revised(d)&&d.level>=7)Add(Potent(d)?"potent":"primal",d.level>=15?"2d8":"1d8");
            if(Revised(d)&&d.level>=15&&Potent(d))Add("potent15");
            if(d.level>=18)Add(Revised(d)?"beastspells24":"beastspells14");
            if(!Revised(d)&&d.level>=18)Add("timeless");
            if(d.level>=20)Add(Revised(d)?"arch24":"arch14");
            if(Is(d,"land")){
                if(!Revised(d)||d.level>=6)Add("land.recovery",(d.level+1)/2);
                if(Revised(d)){Add("land.aid",(d.level>=14?4:d.level>=10?3:2)+"d6");if(d.level>=6)Add("land.free");if(d.level>=10&&!string.IsNullOrEmpty(d.druidLand))Add("land.ward24",Text("resist."+d.druidLand));if(d.level>=14)Add("land.sanctuary24");}
                else{lines.Add(CharacterDescriptionText.Get("druid.land.cantrip"));if(d.level>=6)Add("land.stride");if(d.level>=10)Add("land.ward14");if(d.level>=14)Add("land.sanctuary14");}
            }
            if(Is(d,"moon")){
                if(Revised(d)){Add("moon.forms24",3*d.level);Add("moon.spells");if(d.level>=6)Add("moon.improved");if(d.level>=10)Add("moon.step");if(d.level>=14)Add("moon.lunar");}
                else{Add("moon.combat");lines.Add(CharacterDescriptionText.Get("druid.moon.forms"));if(d.level>=6)Add("moon.magical");if(d.level>=10)Add("moon.elementals");if(d.level>=14)Add("moon.alter");}
            }
            if(Is(d,"sea")){Add("sea.wrath",d.level>=6?10:5);if(d.level>=6)Add("sea.swim");if(d.level>=10)Add("sea.storm");if(d.level>=14)Add("sea.gift");}
            if(Is(d,"stars")){Add("stars.map");Add("stars.form");Add("stars.archer",d.level>=10?"2d8":"1d8");Add("stars.chalice",d.level>=10?"2d8":"1d8");Add("stars.dragon");if(d.level>=6)Add("stars.omen");if(d.level>=10)Add("stars.twinkle");if(d.level>=14)Add("stars.full");}
            return string.Join("\n",lines)+"\n";
        }
    }
}
