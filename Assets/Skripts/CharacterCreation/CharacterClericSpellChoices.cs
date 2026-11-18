using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // The runtime spell catalogue stays outside the standalone rules assembly.
    internal static class CharacterClericSpellChoices
    {
        internal static void Repair(CharacterCreationDraft d)
        {
            if(!CharacterClericRules.Active(d))return;
            var available=CharacterSpellCatalog.AvailableFor(d,1).Select(spell=>spell.name).ToArray();
            for(int i=0;i<d.preparedSpells.Count;i++)if(!string.IsNullOrEmpty(d.preparedSpells[i])&&!available.Contains(d.preparedSpells[i]))d.preparedSpells[i]="";
        }
    }
}
