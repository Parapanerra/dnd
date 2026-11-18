using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationExpandedSpecies
    {
        internal static void Write(CharacterData character,CreationSheetBindings bindings,CharacterCreationDraft d,CreatedCharacterStats stats,Func<string,string> translate)
        {
            string resource=CharacterExpandedSpeciesRules.Resource(d);
            if(resource.Length>0)CreationMagicResources.Add(character,bindings,translate(resource),CharacterExpandedSpeciesRules.ResourceUses(d),CharacterExpandedSpeciesRules.ShortRestResource(d));
            if(d.species=="orclegacy")CreationMagicResources.Add(character,bindings,translate(CharacterExpandedSpeciesRules.Text("pool.relentless")),1,false);
            if(d.species=="aasimarlegacy"&&d.TotalLevel>=3)CreationMagicResources.Add(character,bindings,translate(CharacterExpandedSpeciesRules.Text("pool.revelation")),1,false);
            if(d.species=="khoravar")CreationMagicResources.Add(character,bindings,translate(CharacterExpandedSpeciesRules.Text("pool.lethargy")),1,false,false);
            string dragonExtra=CharacterFtdDragonbornRules.ExtraResource(d);
            if(dragonExtra.Length>0)CreationMagicResources.Add(character,bindings,translate(dragonExtra),1,false);
            if(d.species=="hobgoblin")CreationMagicResources.Add(character,bindings,translate(CharacterExpandedSpeciesRules.Text("pool.gift")),CharacterLegacySpeciesRules.Proficiency(d),false);
            if(d.species=="kender")CreationMagicResources.Add(character,bindings,translate(CharacterExpandedSpeciesRules.Text("pool.fearless")),1,false);
            WriteAttack(character, bindings, d, stats, translate);
        }

        internal static void WriteAttack(CharacterData character, CreationSheetBindings bindings,
            CharacterCreationDraft d, CreatedCharacterStats stats, Func<string, string> translate)
        {
            bool feral=d.species=="lupin";
            bool appendages=CharacterExpandedSpeciesRules.Simic(d,"appendages");
            bool shiftedFangs=d.species=="shifter"&&d.speciesLineage=="longtooth";
            if(!feral&&!appendages&&!shiftedFangs&&!new[]{"tabaxi","dhampir","leonin","aarakocra","centaur","minotaur","satyr","lizardfolk","tortle"}.Contains(d.species))return;
            string name=translate(CharacterExpandedSpeciesRules.Text(feral?"attack.feral":appendages?"attack.appendage":shiftedFangs?"attack.shiftfangs":d.species=="lizardfolk"?"attack.lizardbite":d.species=="dhampir"?"attack.bite":d.species=="aarakocra"?"attack.talons":d.species=="centaur"?"attack.hooves":d.species=="minotaur"?"attack.horns":d.species=="satyr"?"attack.ram":"attack.claws"));
            int modifier=CharacterCreationRules.Modifier(stats.scores[d.species=="dhampir"?2:0]);
            int faces=feral?0:d.species=="dhampir"||d.species=="leonin"?4:6;
            if(feral&&CharacterOriginFeatRules.Has(d,"tavern"))faces=Math.Max(faces,4);
            if(feral&&d.characterClass=="fighter"&&CharacterFighterRules.HasStyle(d,FighterStyle.Unarmed))faces=6;
            if(d.characterClass=="monk")
            {
                faces=Math.Max(faces,CharacterMonkRules.MartialFaces(d));
                if(d.species!="dhampir")modifier=Math.Max(modifier,CharacterCreationRules.Modifier(stats.scores[1]));
            }
            string attack=CharacterCreationRules.Signed(CharacterLegacySpeciesRules.Proficiency(d)+modifier),damage=faces==0?Math.Max(0,1+modifier).ToString():"1d"+faces+CharacterCreationRules.Signed(modifier);
            string Read(int index){var e=bindings.Entries.First(b=>b.key=="input."+index);var data=character.GetSceneData(e.scene);return data.GetString("StableField_v1_"+e.stableId,"");}
            for(int row=0;row<3;row++)
            {
                if(!string.IsNullOrWhiteSpace(Read(67+3*row)))continue;
                bindings.Text(character,"input."+(66+3*row),attack);bindings.Text(character,"input."+(67+3*row),name);bindings.Text(character,"input."+(68+3*row),damage);return;
            }
            string previous=Read(75);bindings.Text(character,"input.75",(previous.Length==0?"":previous+"\n\n")+name+": "+attack+" · "+damage);
        }
    }
}
