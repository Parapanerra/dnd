using System;
namespace Taruck.CharacterCreation
{
    internal static class CreationDruidResources
    {
        internal static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,Func<string,string> t)
        {
            if(!CharacterDruidRules.Active(d))return;
            int uses=CharacterDruidRules.WildUses(d);
            b.Dropdown(c,"main.wild.count",uses<0?9:uses);b.Toggle(c,"rest.main.wild",false);
            if(uses!=0)CreationResourceRecovery.Register(c,b,"rest.main.wild",true,CharacterDruidRules.Revised(d));
            void Add(string key,int count)=>CreationMagicResources.Add(c,b,t(CharacterDruidRules.Text("pool."+key)),count,false,useLastRow:d.IsSecondaryTrack);
            int wisdom=Math.Max(1,CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[4]));
            if(CharacterDruidRules.Is(d,"land")&&(!CharacterDruidRules.Revised(d)||d.level>=6))Add("recovery",1);
            if(CharacterDruidRules.Revised(d)){
                if(CharacterDruidRules.Is(d,"land")&&d.level>=6)Add("free",1);
                if(d.level>=5)Add("resurgence",1);
                if(d.level>=20)Add("arch",1);
                if(CharacterDruidRules.Is(d,"moon")&&d.level>=10)Add("moonstep",wisdom);
                if(CharacterDruidRules.Is(d,"stars")){Add("bolt",wisdom);if(d.level>=6)Add("omen",wisdom);}
            }
        }
    }
}
