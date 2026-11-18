using System;
using System.Linq;
using UnityEngine;
namespace Taruck.CharacterCreation
{
    public sealed partial class CharacterCreationWizard
    {
        private void DrawWarlockChoices()
        {
            ui.Text(content,L(CharacterWarlockRules.Text("ui.invocations")),25,CreationWizardUi.Gold,42);
            Note(CharacterWarlockRules.Text("ui.notes"),75);
            RectTransform row=null;
            for(int i=0;i<CharacterWarlockRules.InvocationCount(draft);i++)
            {
                int slot=i;while(draft.warlockInvocations.Count<=i)draft.warlockInvocations.Add("");
                string[] options=CharacterWarlockInvocations.Options(draft,i);
                DropdownKey(LF(CharacterWarlockRules.Text("ui.invocation"),i+1),options,options.Select(key=>CharacterWarlockInvocations.Name(draft,key,L)).ToArray(),draft.warlockInvocations[i],value=>draft.warlockInvocations[slot]=value,320,SpellChoiceHost(i,ref row));
            }
            for(int i=0;i<CharacterWarlockRules.ArcanumCount(draft);i++)
            {
                int slot=i;while(draft.warlockArcana.Count<=i)draft.warlockArcana.Add("");
                string[] names=CharacterSpellCatalog.Available(draft.edition,"warlock",i+6).Select(s=>s.name).ToArray();
                DropdownKey(LF(CharacterWarlockRules.Text("ui.arcana"),i+6),names,names.Select(L).ToArray(),draft.warlockArcana[i],value=>draft.warlockArcana[slot]=value,320);
            }
            if(CharacterWarlockRules.Is(draft,"fiend")&&draft.level>=10)
                DropdownInt(L(CharacterWarlockRules.Text("ui.resistance")),Enumerable.Range(0,CharacterWarlockRules.ResistanceTypes.Length).Where(i=>!CharacterWarlockRules.Modern(draft)||CharacterWarlockRules.ResistanceTypes[i]!="Силова").ToArray(),draft.warlockResistance,value=>draft.warlockResistance=value,i=>CharacterWarlockRules.ResistanceTypes[i],280);
        }
    }
}
