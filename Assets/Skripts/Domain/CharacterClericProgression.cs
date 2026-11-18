using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterClericRules
    {
        public static bool Active(CharacterCreationDraft d)=>d.characterClass=="cleric";
        public static bool Revised(CharacterCreationDraft d)=>d.edition==RulesEdition.Rules2024;
        public static int SubclassLevel(CharacterCreationDraft d)=>Revised(d)?3:1;
        public static string[] Options(CharacterCreationDraft d)=>Revised(d)?new[]{"life","light","trickery","war"}:Domains;
        public static bool Is(CharacterCreationDraft d,string id)=>Active(d)&&d.level>=SubclassLevel(d)&&d.subclass==id&&Options(d).Contains(id);
        internal static int Cantrips(CharacterCreationDraft d)=>(d.level>=10?5:d.level>=4?4:3)+(Revised(d)&&d.divineOrder=="thaumaturge"?1:0);
        internal static int SpellCount(CharacterCreationDraft d)=>Revised(d)?new[]{4,5,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22}[Math.Clamp(d.level,1,20)-1]:Math.Max(1,d.level+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[4]));
        public static int MaximumCircle(CharacterCreationDraft d)=>Math.Min(9,(d.level+1)/2);
        internal static int Slots(CharacterCreationDraft d,int circle)=>Active(d)?CharacterFullCasterSlots.Count(d.level,circle):0;
        public static int ChannelUses(CharacterCreationDraft d)=>d.level<2?0:Revised(d)?d.level>=18?4:d.level>=6?3:2:d.level>=18?3:d.level>=6?2:1;
        public static string Text(string id)=>CharacterClericText.Get(id);
        internal static bool Potent(CharacterCreationDraft d)=>Active(d)&&(Revised(d)?d.level>=7&&d.clericStrike=="potent":d.level>=8&&(Is(d,"knowledge")||Is(d,"light")));
        public static string Features(CharacterCreationDraft d)
        {
            var lines=new List<string>();void Add(string id,params object[] args)=>lines.Add(args.Length==0?Text(id):string.Format(Text(id),args));
            if(Revised(d))Add(d.divineOrder=="protector"?"order.protector":"order.thaumaturge");
            if(d.level>=2){Add(Revised(d)?"channel24":"channel14",ChannelUses(d));Add(Revised(d)?"turn24":"turn14");if(Revised(d))Add("spark",(d.level>=18?4:d.level>=13?3:d.level>=7?2:1)+"d8");}
            if(d.level>=5)Add(Revised(d)?"sear":"destroy",d.level>=17?"4":d.level>=14?"3":d.level>=11?"2":d.level>=8?"1":"1/2");
            if(Revised(d)&&d.level>=7)Add(d.clericStrike=="potent"?"potent":"strike24",d.level>=14?"2d8":"1d8");
            if(Revised(d)&&d.level>=14&&d.clericStrike=="potent")Add("potent14");
            if(d.level>=10){Add(Revised(d)?"intervention24":d.level>=20?"intervention20":"intervention14");if(Revised(d)&&d.level>=20)Add("wish");}
            if(d.level<SubclassLevel(d))return string.Join("\n",lines)+"\n";
            if(!Revised(d))
            {
                lines.Add(DomainFeature(d.subclass));
                if(d.subclass=="life")lines.Add(CharacterDescriptionText.Get("cleric.domain.heavy"));
                if(d.subclass=="tempest"||d.subclass=="war")lines.Add(CharacterDescriptionText.Get("cleric.domain.martial"));
                if(d.subclass=="light")lines.Add(CharacterDescriptionText.Get("cleric.domain.light"));
            }
            switch(d.subclass) {
                case "life":
                    if(Revised(d))Add("life.disciple24");
                    if(d.level>=2)Add(Revised(d)?"life.preserve24":"life.preserve14",5*d.level);
                    if(d.level>=6)Add(Revised(d)?"life.healer24":"life.healer14");
                    if(d.level>=17)Add(Revised(d)?"life.supreme24":"life.supreme14");break;
                case "light":
                    if(Revised(d))Add("light.flare24");
                    if(d.level>=2)Add(Revised(d)?"light.radiance24":"light.radiance14",d.level);
                    if(d.level>=6)Add(Revised(d)?"light.improved24":"light.improved14");
                    if(d.level>=17)Add(Revised(d)?"light.corona24":"light.corona14");break;
                case "trickery":
                    if(Revised(d))Add("trickery.bless24");
                    if(d.level>=2)Add(Revised(d)?"trickery.duplicate24":"trickery.duplicate14");
                    if(d.level>=6)Add(Revised(d)?"trickery.transpose":"trickery.cloak");
                    if(d.level>=17)Add(Revised(d)?"trickery.improved24":"trickery.improved14",d.level);break;
                case "war":
                    if(Revised(d))Add("war.priest24");
                    if(d.level>=2)Add(Revised(d)?"war.guided24":"war.guided14");
                    if(d.level>=6)Add(Revised(d)?"war.bless24":"war.bless14");
                    if(d.level>=17)Add(Revised(d)?"war.avatar24":"war.avatar14");break;
                case "knowledge":
                    if(d.level>=2)Add("knowledge.ages");if(d.level>=6)Add("knowledge.thoughts");if(d.level>=17)Add("knowledge.visions");break;
                case "nature":
                    if(d.level>=2)Add("nature.charm");if(d.level>=6)Add("nature.dampen");if(d.level>=17)Add("nature.master");break;
                case "tempest":
                    if(d.level>=2)Add("tempest.wrath");if(d.level>=6)Add("tempest.push");if(d.level>=17)Add("tempest.flight");break;
            }
            if(!Revised(d)&&d.level>=8) {
                if(Potent(d))Add("potent");
                else Add("strike14",d.level>=14?"2d8":"1d8",Text("damage."+d.subclass));
            }
            return string.Join("\n",lines)+"\n";
        }
    }
}
