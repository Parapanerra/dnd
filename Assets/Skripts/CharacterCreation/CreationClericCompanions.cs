using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    internal static class CreationClericCompanions
    {
        internal static ReadyFormTemplate[] Templates(CharacterCreationDraft d,CreatedCharacterStats stats)
        {
            if(d.edition!=RulesEdition.Rules2024||!d.preparedSpells.Concat(d.bardSecrets).Concat(CharacterWarlockRules.AutomaticSpells(d)).Contains("Summon Celestial"))return Array.Empty<ReadyFormTemplate>();
            return new[]{false,true}.Select(defender=>new ReadyFormTemplate("summon.celestial."+(defender?"defender":"avenger"),CharacterClericRules.Text(defender?"form.defender":"form.avenger"),(c,b,slot,t)=>Write(c,b,d,stats,slot,defender,t))).ToArray();
        }
        private static void Write(CharacterData c,CreationSheetBindings b,CharacterCreationDraft d,CreatedCharacterStats stats,int slot,bool defender,Func<string,string> t)
        {
            string scene=WildShapeFormList.SceneNameForSlot(slot);void Field(int i,string value)=>b.Text(c,"form.input."+i,value,scene);
            string T(string key)=>t(CharacterClericRules.Text(key));int modifier=CharacterCreationRules.Modifier(stats.scores[CharacterWarlockRules.Active(d)||CharacterBardRules.Active(d)||CharacterSorcererRules.Active(d)?5:CharacterClericRules.Active(d)||CharacterRangerRules.Active(d)?4:3]);int pb=CharacterLegacySpeciesRules.Proficiency(d);
            Field(1,T(defender?"form.defender":"form.avenger"));Field(0,CharacterCreationRules.Signed(modifier));Field(44,(8+pb+modifier).ToString());Field(3,CharacterCreationRules.Signed(pb));Field(4,"12");Field(6,"+2");Field(7,"30");Field(8,defender?"18":"16");
            int[] scores={16,14,16,10,14,16};for(int i=0;i<6;i++){Field(9+2*i,CharacterCreationRules.Signed(CharacterCreationRules.Modifier(scores[i])));Field(10+2*i,scores[i].ToString());}
            Field(21,CharacterCreationRules.Signed(pb+modifier));Field(22,T(defender?"form.mace":"form.bow"));Field(23,defender?"1d10+8":"2d6+7");Field(2,T("form.common"));Field(30,T(defender?"form.mace.rules":"form.bow.rules"));Field(32,T("form.heal"));Field(33,"2d8+5");Field(40,T("form.heal.rules"));
            Field(41, ReadyFormProgressionNotes.SlotGrowth(5, 40, defender ? 18 : 16, 10, defender ? "1d10+8" : "2d6+7", t));
            Field(42, t("Атак за дію = половина круга комірки, округлення вниз.") + "\n\n" +
                t("Після зміни комірки або показників мага онови поля форми вручну."));
            var state=c.GetSceneData(scene);state.SetInt(b.FormHealthPrefix+"maxHealth",40);state.SetInt(b.FormHealthPrefix+"currentHealth",40);state.SetInt(b.FormHealthPrefix+"maxTemporaryHealth",0);state.SetInt(b.FormHealthPrefix+"currentTemporaryHealth",0);
        }
    }
}
