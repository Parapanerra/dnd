using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterWeaponProficiency
    {
        public static bool Has(CharacterCreationDraft d,CoreWeapon weapon,bool includeTrance=true)
        {
            if (d.secondaryClass?.IsSelected == true && CharacterSecondaryClass.GrantsWeapon(d.secondaryClass.ClassDraft(d.edition), weapon))
                return true;
            if(includeTrance&&Enumerable.Range(0,CharacterExpandedSpeciesRules.TranceCount(d)).Any(i=>CharacterExpandedSpeciesRules.Trance(d,i)=="weapon:"+weapon.name))return true;
            if(d.species=="giff"&&(weapon.name=="Пістоль"||weapon.name=="Мушкет"))return true;
            if(d.edition==RulesEdition.Rules2014)
            {
                if(CharacterSpeciesRules.IsDwarf(d)&&new[]{"Бойова сокира","Ручна сокира","Легкий молот","Бойовий молот"}.Contains(weapon.name))return true;
                if(d.species=="elf"&&(d.speciesLineage=="drow"?new[]{"Рапіра","Короткий меч","Ручний арбалет"}:new[]{"Довгий меч","Короткий меч","Короткий лук","Довгий лук"}).Contains(weapon.name))return true;
            }
            switch(d.characterClass)
            {
                case "wizard":case "sorcerer":return d.edition==RulesEdition.Rules2024?weapon.simple:new[]{"Кинджал","Дротик","Праща","Бойовий посох","Легкий арбалет"}.Contains(weapon.name);
                case "druid":return d.edition==RulesEdition.Rules2024?weapon.simple||d.primalOrder=="warden":new[]{"Дубина","Кинджал","Дротик","Метальний спис","Булава","Бойовий посох","Скімітар","Серп","Праща","Спис"}.Contains(weapon.name);
                case "cleric":return weapon.simple||CharacterClericRules.MartialWeapons(d);
                case "warlock":return weapon.simple;
                case "monk":return weapon.simple||(d.edition==RulesEdition.Rules2024?weapon.light:weapon.name=="Короткий меч");
                case "rogue":return weapon.simple||(d.edition==RulesEdition.Rules2024?weapon.finesse||weapon.light:new[]{"Ручний арбалет","Довгий меч","Рапіра","Короткий меч"}.Contains(weapon.name));
                case "bard":return weapon.simple||CharacterBardRules.Is(d,"valor")||d.edition==RulesEdition.Rules2014&&new[]{"Ручний арбалет","Довгий меч","Рапіра","Короткий меч"}.Contains(weapon.name);
                default:return true;
            }
        }
    }
}
