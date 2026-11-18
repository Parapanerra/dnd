using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterBloodHunterRules
    {
        public static readonly string[] FormulaIds = { "alluring", "celerity", "conversant", "cruelty", "deftness", "embers", "gelid", "impermeable", "mobility", "nighteye", "percipient", "potency", "precision", "rapidity", "reconstruction", "sagacity", "shielded", "unbreakable", "vermillion", "aether" };
        internal static int FormulaLevel(string id) => id == "reconstruction" ? 7 : new[] { "cruelty", "precision", "aether" }.Contains(id) ? 11 : 3;
        public static string FormulaDescription(string id) => Text("formula."+id);
        public static string FormulaName(string id) => FormulaDescription(id).Split(':')[0];
        internal static bool ValidFormulas(CharacterCreationDraft d,IEnumerable<string> values)
        {
            string[] ids=values.Where(s=>!string.IsNullOrEmpty(s)).ToArray();
            if(ids.Distinct().Count()!=ids.Length || ids.Any(id=>!FormulaIds.Contains(id)||FormulaLevel(id)>d.level))return false;
            // At each formula acquisition one new formula and one replacement are permitted.
            foreach(int threshold in new[]{7,11}.Where(l=>l<=d.level))
                if(ids.Count(id=>FormulaLevel(id)>=threshold)>2*new[]{7,11,15,18}.Count(l=>l>=threshold&&l<=d.level))return false;
            return true;
        }
        public static string[] FormulaOptions(CharacterCreationDraft d,int slot) => FormulaIds.Where(id=>ValidFormulas(d,d.bloodFormulas.Where((_,i)=>i!=slot).Concat(new[]{id}))).ToArray();
    }
}
