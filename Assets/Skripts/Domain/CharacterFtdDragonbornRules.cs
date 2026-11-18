using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterFtdDragonbornRules
    {
        public static readonly string[] Families={"chromatic","metallic","gem"};
        public static bool Active(CharacterCreationDraft d)=>d.species=="dragonbornftd";
        public static string Text(string key)=>CharacterExpandedSpeciesRules.Text("ftd."+key);
        internal static bool Chosen(CharacterCreationDraft d)=>Families.Contains(d.speciesLineage)&&d.speciesAncestor>=0&&d.speciesAncestor<5;
        public static string Ancestor(CharacterCreationDraft d,int index)=>Families.Contains(d.speciesLineage)&&index>=0&&index<5?Text(d.speciesLineage+"."+index):"";
        internal static string LineageName(CharacterCreationDraft d)=>Chosen(d)?Text("lineage."+d.speciesLineage+"."+d.speciesAncestor):"";
        internal static string Damage(CharacterCreationDraft d)
        {
            if(!Chosen(d))return "";
            string[] types=d.speciesLineage=="chromatic"?new[]{"acid","lightning","poison","fire","cold"}:d.speciesLineage=="metallic"?new[]{"fire","lightning","acid","fire","cold"}:new[]{"force","radiant","psychic","thunder","necrotic"};
            return Text("damage."+types[d.speciesAncestor]);
        }
        internal static int DiceCount(CharacterCreationDraft d)=>d.TotalLevel>=17?4:d.TotalLevel>=11?3:d.TotalLevel>=5?2:1;
        public static string Dice(CharacterCreationDraft d)=>DiceCount(d)+"d10";
        internal static int SaveDc(CharacterCreationDraft d)=>8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2]);
        internal static string Features(CharacterCreationDraft d)
        {
            if(!Active(d))return "";
            string result=Text("traits.root");
            if(!Chosen(d))return result;
            result+="\n"+string.Format(Text("traits.resist"),Damage(d));
            result+="\n"+(d.abilities.All(value=>value>0)?string.Format(Text("traits.breath"),Text(d.speciesLineage=="chromatic"?"shape.line":"shape.cone"),Damage(d),SaveDc(d)):Text("traits.breathpending"));
            if(d.speciesLineage=="gem")result+="\n"+Text("traits.telepathy");
            if(d.TotalLevel>=5)result+="\n"+(d.speciesLineage=="chromatic"?string.Format(Text("traits.warding"),Damage(d)):Text(d.speciesLineage=="gem"?"traits.flight":"traits.metallic"));
            return result;
        }
        public static string ExtraResource(CharacterCreationDraft d)=>!Active(d)||!Chosen(d)||d.TotalLevel<5?"":Text("pool."+d.speciesLineage);
    }
}
