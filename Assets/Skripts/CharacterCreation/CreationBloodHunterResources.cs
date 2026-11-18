using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationBloodHunterResources
    {
        internal static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,Func<string,string> t)
        {
            if(!CharacterBloodHunterRules.Active(d))return;
            string Add(string key,int uses,bool shortRest=false)=>CreationMagicResources.Add(c,b,t(CharacterBloodHunterRules.Text("pool."+key)),uses,shortRest,useLastRow:d.IsSecondaryTrack);
            var rites=d.bloodRites.Where(id=>!string.IsNullOrEmpty(id)).Concat(CharacterBloodHunterRules.Is(d,"ghostslayer")?new[]{"dawn"}:Array.Empty<string>()).ToArray();
            for(int i=0;i<4;i++) {
                b.Text(c,"main.blood.rite."+i+".name",i<rites.Length?t(CharacterBloodHunterRules.Text("rite."+rites[i])):"");
                b.Toggle(c,"main.blood.rite."+i+".active",false);
            }
            CreationResourceRecovery.RegisterPool(c,b,"rest.blood.rites",Enumerable.Range(0,4).Select(i=>"main.blood.rite."+i+".active").ToArray(),true,false);
            if(d.level>=6)Add("brand",1,true);
            if(CharacterBloodHunterRules.Is(d,"ghostslayer") && d.level>=7)Add("step",d.level>=15?2:1,true);
            if(CharacterBloodHunterRules.Is(d,"lycan") && d.level<18)Add("hybrid",d.level>=11?2:1,true);
            if(CharacterBloodHunterRules.Is(d,"mutant")) {
                Add("mutagens",CharacterBloodHunterRules.Mutagens(d),true);
                if(d.level>=7)Add("metabolism",1);
                if(d.level>=18)Add("exalted",Math.Max(1,CharacterBloodHunterRules.HemocraftModifier(d)));
            }
            if(CharacterBloodHunterRules.Profane(d)) {
                if(d.level>=7)Add("revealed",1);
                if(d.level>=15)Add("unsealed",1);
                if(d.level>=18)Add("souleater",1);
            }
            // Rites are known choices, not preactivated buffs. Their HP cost and duration require play decisions.
            if(!d.IsSecondaryTrack && d.level>=2 && d.fightingStyle==FighterStyle.Archery && IsRanged(d.bloodWeapon)) {
                int mod=CharacterBloodHunterRules.WeaponModifier(d,CharacterCreationRules.Scores(d));
                b.Text(c,"input.66",CharacterCreationRules.Signed(CharacterLegacySpeciesRules.Proficiency(d)+mod+2));
            }
        }
        private static bool IsRanged(string weapon)=>new[]{"Ручний арбалет","Важкий арбалет","Довгий лук","Духова трубка","Сітка","Легкий арбалет","Короткий лук","Праща","Дротик"}.Contains(weapon);
    }
}
