using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Alternative AC calculations, never added to one another. Starter items remain owned.
    public static class CharacterNaturalArmor
    {
        public static bool Active(CharacterCreationDraft d)=>new[]{"lizardfolk","tortle","locathah","loxodon","autognome","thrikreen","simic"}.Contains(d.species);
        internal static bool WearsArmor(CharacterCreationDraft d)=>!Active(d)||d.species!="tortle"&&!d.expandedUnarmored&&StarterArmor(d);
        public static bool StarterArmor(CharacterCreationDraft d)
        {
            if(new[]{"barbarian","monk","wizard","sorcerer"}.Contains(d.characterClass))return false;
            if (d.characterClass == "fighter") return !CharacterFighterRules.TakesStartingGold(d);
            if (d.characterClass == "bloodhunter") return true;
            return d.edition==RulesEdition.Rules2014||d.equipment==0;
        }
        internal static int StarterShield(CharacterCreationDraft d)
        {
            bool modern=d.edition==RulesEdition.Rules2024;
            if(d.characterClass=="fighter")return !modern&&d.equipment==0?2:0;
            if(d.characterClass=="paladin")return d.equipment==0?2:0;
            if(d.characterClass=="cleric")return !modern||d.equipment==0?2:0;
            if(d.characterClass=="druid")return (modern?d.equipment==0:d.druidFirstItem=="Дерев’яний щит"&&d.druidWeapon!="Велика дубина")?2:0;
            return 0;
        }
        internal static int NaturalBase(CharacterCreationDraft d,int[] scores)=>d.species=="simic"?10+CharacterCreationRules.Modifier(scores[1]):d.species=="tortle"?17:(d.species=="locathah"||d.species=="loxodon"?12:13)+CharacterCreationRules.Modifier(scores[d.species=="loxodon"?2:1]);
        internal static CreatedCharacterStats Apply(CharacterCreationDraft d,CreatedCharacterStats s)
        {
            s=ApplyBase(d,s);
            if(CharacterExpandedSpeciesRules.Simic(d,"carapace")&&(!WearsArmor(d)||!HeavyStarter(d)))s.armorClass++;
            return s;
        }
        internal static bool HeavyStarter(CharacterCreationDraft d)=>d.characterClass=="fighter"&&d.equipment==0||d.characterClass=="paladin"&&(d.edition==RulesEdition.Rules2014||d.equipment==0)||d.characterClass=="cleric"&&d.edition==RulesEdition.Rules2014&&d.clericArmor=="Кольчуга";
        private static CreatedCharacterStats ApplyBase(CharacterCreationDraft d,CreatedCharacterStats s)
        {
            if(!Active(d))return s;
            bool wearing=WearsArmor(d) || CharacterArtificerRules.Active(d) && CharacterArtificerEquipment.CreatedArmorBase(d) > 0;
            int shield=StarterShield(d),shieldMagic=CharacterArtificerEquipment.Bonus(d,"shield");
            if(shieldMagic>0)shield=Math.Max(shield,2+shieldMagic);
            int protection=CharacterArtificerEquipment.ProtectionBonus(d);
            bool naturalWhileArmored=new[]{"lizardfolk","locathah","loxodon"}.Contains(d.species);
            if(wearing&&!naturalWhileArmored)return s;
            int alternative=NaturalBase(d,s.scores);
            if(wearing)
            {
                bool defense=d.characterClass=="fighter"&&CharacterFighterRules.HasStyle(d,FighterStyle.Defense)
                    ||new[]{"ranger","paladin"}.Contains(d.characterClass)&&d.level>=2&&d.fightingStyle==FighterStyle.Defense;
                alternative+=defense?1:0;
                alternative+=CharacterArtificerEquipment.WornArmorBonus(d);
                s.armorClass=Math.Max(s.armorClass,alternative+shield+protection);
                return s;
            }
            int dex=CharacterCreationRules.Modifier(s.scores[1]);
            alternative=Math.Max(alternative,10+dex);
            if(d.characterClass=="barbarian")alternative=Math.Max(alternative,10+dex+CharacterCreationRules.Modifier(s.scores[2]));
            if(d.characterClass=="monk"&&shield==0)alternative=Math.Max(alternative,10+dex+CharacterCreationRules.Modifier(s.scores[4]));
            if(d.characterClass=="bard"&&CharacterBardRules.Is(d,"dance")&&shield==0)alternative=Math.Max(alternative,10+dex+CharacterCreationRules.Modifier(s.scores[5]));
            if(d.characterClass=="sorcerer"&&CharacterSorcererRules.Draconic(d))alternative=Math.Max(alternative,d.edition==RulesEdition.Rules2014?13+dex:10+dex+Math.Max(0,CharacterCreationRules.Modifier(s.scores[5])));
            s.armorClass=alternative+shield+protection;
            bool heavy=d.characterClass=="fighter"&&d.equipment==0
                ||d.characterClass=="paladin"&&(d.edition==RulesEdition.Rules2014||d.equipment==0)
                ||d.characterClass=="cleric"&&d.edition==RulesEdition.Rules2014&&d.clericArmor=="Кольчуга";
            if(StarterArmor(d)&&heavy&&s.scores[0]<13)s.speed+=10;
            return s;
        }
    }
}
