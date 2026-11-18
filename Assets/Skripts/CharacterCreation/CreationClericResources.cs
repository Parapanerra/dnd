using System;

namespace Taruck.CharacterCreation
{
    internal static class CreationClericResources
    {
        internal static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,Func<string,string> t)
        {
            if(!CharacterClericRules.Active(d))return;
            int channel=CharacterClericRules.ChannelUses(d);
            if (!d.IsSecondaryTrack)
            {
                b.Dropdown(c,"main.channel.count",channel);b.Toggle(c,"rest.main.channel",false);
                if(channel>0)CreationResourceRecovery.Register(c,b,"rest.main.channel",true,CharacterClericRules.Revised(d));
            }
            string Add(string key,int count,bool shortRest=false)=>CreationMagicResources.Add(c,b,t(CharacterClericRules.Text("pool."+key)),count,shortRest,useLastRow:d.IsSecondaryTrack);
            int wisdom=Math.Max(1,CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[4]));
            if(CharacterClericRules.Is(d,"light"))Add("flare",wisdom,CharacterClericRules.Revised(d)&&d.level>=6);
            if(CharacterClericRules.Is(d,"tempest"))Add("tempest",wisdom);
            if(CharacterClericRules.Is(d,"war"))Add("priest",wisdom,CharacterClericRules.Revised(d));
            if(d.level>=10) {
                string key=Add("intervention",1);
                // A successful PHB intervention (7 days) or XPHB Wish (2d4 rests) has a variable cooldown.
                // Keep that counter manual rather than incorrectly restoring it on every long rest.
                if(!CharacterClericRules.Revised(d)||d.level>=20) {
                    CreationResourceRecovery.Unregister(c,key);
                }
            }
            if(CharacterClericRules.Is(d,"knowledge")&&d.level>=17)Add("visions",1,true);
            if(CharacterClericRules.Is(d,"light")&&CharacterClericRules.Revised(d)&&d.level>=17)Add("corona",wisdom);
        }
    }
}
