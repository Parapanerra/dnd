using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationMasteryDescriptions
    {
        internal static IEnumerable<string> For(CharacterCreationDraft d,Func<string,string> t)
        {
            if(d.edition!=RulesEdition.Rules2024)yield break;
            var groups=new Dictionary<string,List<string>>();
            void Add(string property,string weapon)
            {
                if(!groups.TryGetValue(property,out var names))groups[property]=names=new List<string>();
                if(!names.Contains(weapon))names.Add(weapon);
            }
            foreach(int weapon in d.weaponMasteries.Where(i=>i>=0&&i<CharacterCreationRules.MasteryWeapons.Count))
            {
                string name=CharacterCreationRules.MasteryWeapons[weapon];
                var definition=CharacterWeaponCatalog.Find(name,RulesEdition.Rules2024);
                if(definition!=null&&definition.mastery.Length>0)Add(definition.mastery,t(name));
            }
            if(d.characterClass=="fighter"&&d.level>=9)
                foreach(string property in new[]{"push","sap","slow"})Add(property,t("Тактичний майстер"));
            if(CharacterRogueRules.Is(d,"soulknife"))Add("vex",t("Психічні клинки"));
            foreach(var group in groups)yield return string.Join(", ",group.Value)+" — "+t(CharacterDescriptionText.Get("mastery."+group.Key));
        }
    }
}
