using System;
using System.Linq;
using UnityEngine;

namespace Taruck.CharacterCreation
{
    [Serializable] internal sealed class DruidBeastAttack { public string name, damage; public int bonus; }
    [Serializable] internal sealed class DruidBeastBonus { public int index, bonus; }
    [Serializable] internal sealed class DruidBeast
    {
        public string id, name, sourceName, notes;
        public int edition, ac, hp, speed, fly, swim, climb, burrow, passive;
        public double cr;
        public bool elemental;
        public int[] scores;
        public DruidBeastAttack[] attacks;
        public DruidBeastBonus[] skills, saves;
    }
    [Serializable] internal sealed class DruidBeastData { public DruidBeast[] entries; }

    internal static class CharacterDruidFormChoices
    {
        private static readonly Lazy<DruidBeast[]> Catalog = new Lazy<DruidBeast[]>(() =>
            JsonUtility.FromJson<DruidBeastData>(Resources.Load<TextAsset>("CharacterCreation/DruidBeasts").text).entries);
        internal static System.Collections.Generic.IEnumerable<DruidBeast> All => Catalog.Value;
        internal static DruidBeast[] Options(CharacterCreationDraft d) => !CharacterDruidRules.Active(d)||d.level<2 ? Array.Empty<DruidBeast>() : All.Where(f=>f.edition==(CharacterDruidRules.Revised(d)?2024:2014)&&
            (f.elemental ? CharacterDruidRules.Is(d,"moon")&&!CharacterDruidRules.Revised(d)&&d.level>=10 : f.cr<=CharacterDruidRules.MaxCR(d)&&(f.fly==0||CharacterDruidRules.Flying(d))&&(f.swim==0||CharacterDruidRules.Swimming(d)))).OrderBy(f=>f.cr).ThenBy(f=>f.name,StringComparer.Ordinal).ToArray();
        internal static void Repair(CharacterCreationDraft d)
        {
            if(!CharacterDruidRules.Active(d))return;
            string[] spells=CharacterSpellCatalog.AvailableFor(d,1).Select(s=>s.name).ToArray();
            for(int i=0;i<d.preparedSpells.Count;i++)if(d.preparedSpells[i].Length>0&&!spells.Contains(d.preparedSpells[i]))d.preparedSpells[i]="";
            string[] cantrips=CharacterSpellCatalog.AvailableFor(d,0).Select(s=>s.name).ToArray();
            for(int i=0;i<d.cantrips.Count;i++)if(d.cantrips[i].Length>0&&!cantrips.Contains(d.cantrips[i]))d.cantrips[i]="";
            var forms=Options(d);var used=new System.Collections.Generic.HashSet<string>();
            for(int i=0;i<d.druidKnownForms.Count;i++)if(!forms.Any(f=>!f.elemental&&f.id==d.druidKnownForms[i])||!used.Add(d.druidKnownForms[i]))d.druidKnownForms[i]="";
        }
        internal static string Error(CharacterCreationDraft d) => CharacterDruidRules.Active(d)&&CharacterDruidRules.KnownForms(d)>0&&
            (d.druidKnownForms==null||d.druidKnownForms.Count!=CharacterDruidRules.KnownForms(d)||d.druidKnownForms.Distinct().Count()!=d.druidKnownForms.Count||d.druidKnownForms.Any(id=>!Options(d).Any(f=>!f.elemental&&f.id==id))) ? CharacterDruidRules.Text("error.forms"):null;
    }
}
