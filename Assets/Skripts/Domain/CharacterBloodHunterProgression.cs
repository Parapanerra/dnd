using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static partial class CharacterBloodHunterRules
    {
        public static readonly string[] Orders = { "ghostslayer", "profane", "mutant", "lycan" };
        public static readonly string[] Patrons = { "archfey", "greatoldone", "fiend", "undead", "celestial", "fathomless", "undying", "genie", "hexblade" };
        public static readonly string[] RiteIds = { "storm", "frozen", "flame", "dead", "oracle", "roar" };
        public static readonly int[] Styles = { (int)FighterStyle.Archery, (int)FighterStyle.Dueling, (int)FighterStyle.GreatWeapon, (int)FighterStyle.TwoWeapon };
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "bloodhunter";
        public static bool Is(CharacterCreationDraft d, string id) => Active(d) && d.level >= 3 && d.subclass == id;
        public static bool Profane(CharacterCreationDraft d) => Is(d, "profane");
        public static string Text(string key) => CharacterBloodHunterText.Get(key);
        public static int Die(CharacterCreationDraft d) => d.level >= 17 ? 10 : d.level >= 11 ? 8 : d.level >= 5 ? 6 : 4;
        internal static int CursesKnown(CharacterCreationDraft d) => 1 + new[] { 6, 10, 14, 18 }.Count(l => d.level >= l);
        public static int CurseUses(CharacterCreationDraft d) => (d.level >= 17 ? 4 : d.level >= 13 ? 3 : d.level >= 6 ? 2 : 1) + (Is(d, "ghostslayer") ? 1 : 0);
        internal static int RitesKnown(CharacterCreationDraft d) => d.level < 2 ? 0 : d.level >= 14 ? 3 : d.level >= 7 ? 2 : 1;
        internal static int FormulasKnown(CharacterCreationDraft d) => Is(d, "mutant") ? 4 + new[] { 7, 11, 15, 18 }.Count(l => l <= d.level) : 0;
        public static int Mutagens(CharacterCreationDraft d) => d.level >= 15 ? 3 : d.level >= 7 ? 2 : 1;
        internal static int Cantrips(CharacterCreationDraft d) => Profane(d) ? d.level >= 10 ? 3 : 2 : 0;
        internal static int SpellCount(CharacterCreationDraft d) => !Profane(d) ? 0 : new[] { 0, 0, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 11 }[Math.Clamp(d.level,1,20)-1];
        public static int PactCircle(CharacterCreationDraft d) => !Profane(d) ? 0 : d.level >= 19 ? 4 : d.level >= 13 ? 3 : d.level >= 7 ? 2 : 1;
        internal static int PactSlots(CharacterCreationDraft d) => !Profane(d) ? 0 : d.level >= 6 ? 2 : 1;
        public static bool CanLearn(CharacterCreationDraft d,IEnumerable<int> circles)
        {
            int[] values=circles.ToArray();
            if(values.Any(l=>l<1||l>PactCircle(d)))return false;
            foreach(int threshold in new[]{7,13,19}.Where(l=>l<=d.level))
            {
                var before=new CharacterCreationDraft {characterClass="bloodhunter",subclass="profane",level=threshold-1};
                if(values.Count(l=>l>PactCircle(before))>SpellCount(d)-SpellCount(before)+d.level-threshold+1)return false;
            }
            return true;
        }
        public static int HemocraftModifier(CharacterCreationDraft d) => CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[d.bloodAbility == 4 ? 4 : 3]);
        public static string[] AutomaticSpells(CharacterCreationDraft d)
        {
            int index = Array.IndexOf(Patrons,d.bloodPatron);
            if (!Profane(d) || index < 0) return Array.Empty<string>();
            var spells = new List<string>();
            if (d.level >= 7) spells.Add(new[] { "Blur", "Detect Thoughts", "Scorching Ray", "Blindness/Deafness", "Lesser Restoration", "Gust of Wind", "Silence", "Phantasmal Force", "Branding Smite" }[index]);
            if (d.level >= 15) spells.Add(new[] { "Slow", "Haste", "Fireball", "Speak with Dead", "Revivify", "Lightning Bolt", "Bestow Curse", "Protection from Energy", "Blink" }[index]);
            return spells.ToArray();
        }
        internal static bool ValidRites(CharacterCreationDraft d, IEnumerable<string> values)
        {
            string[] ids=values.Where(s=>!string.IsNullOrEmpty(s)).ToArray();
            return ids.Distinct().Count()==ids.Length && ids.All(RiteIds.Contains) && ids.Count(s=>Array.IndexOf(RiteIds,s)>=3) <= (d.level>=14?1:0);
        }
        public static string[] RiteOptions(CharacterCreationDraft d,int slot) => RiteIds.Where(id => (slot >= 2 || Array.IndexOf(RiteIds,id)<3) && !d.bloodRites.Where((_,i)=>i!=slot).Contains(id)).ToArray();
        public static string[] CurseOptions(CharacterCreationDraft d,int slot) => CurseIds.Where(id=>!d.bloodCurses.Where((_,i)=>i!=slot).Contains(id)).ToArray();
        private static void Resize(List<string> values,int count) { while(values.Count<count)values.Add(""); if(values.Count>count)values.RemoveRange(count,values.Count-count); }
        internal static void Repair(CharacterCreationDraft d)
        {
            d.bloodCurses ??= new List<string>(); d.bloodRites ??= new List<string>(); d.bloodFormulas ??= new List<string>();
            if(!Active(d)) { d.bloodCurses.Clear();d.bloodRites.Clear();d.bloodFormulas.Clear();d.bloodPatron="";return; }
            if(d.bloodCurses.Count==0 && CurseIds.Contains(d.bloodCurse))d.bloodCurses.Add(d.bloodCurse);
            Resize(d.bloodCurses,CursesKnown(d)); Resize(d.bloodRites,RitesKnown(d)); Resize(d.bloodFormulas,FormulasKnown(d));
            for(int i=0;i<d.bloodCurses.Count;i++) if(!CurseOptions(d,i).Contains(d.bloodCurses[i]))d.bloodCurses[i]="";
            for(int i=0;i<d.bloodRites.Count;i++) if(!RiteOptions(d,i).Contains(d.bloodRites[i]))d.bloodRites[i]="";
            for(int i=0;i<d.bloodFormulas.Count;i++)if(!FormulaOptions(d,i).Contains(d.bloodFormulas[i]))d.bloodFormulas[i]="";
            d.bloodCurse=d.bloodCurses.FirstOrDefault()??"";
            if(!Profane(d))d.bloodPatron="";
            if(d.level<3)d.subclass="";
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var lines=new List<string>();
            void Add(string key,params object[] args) => lines.Add(args.Length==0?Text(key):string.Format(Text(key),args));
            Add("base.hunter"); Add("base.blood",CurseUses(d),"1d"+Die(d));
            foreach(string id in d.bloodCurses.Where(CurseIds.Contains))lines.Add(new[]{"eyeless","puppet","marked"}.Contains(id)?Text("curse."+id):CurseText(id));
            if(d.level>=2) Add("base.rite","1d"+Die(d));
            if(d.level>=2) foreach(string rite in d.bloodRites.Where(RiteIds.Contains))lines.Add(CharacterDescriptionText.Get("rite."+rite));
            if(d.level>=2 && Styles.Contains((int)d.fightingStyle))
                lines.Add(d.fightingStyle==FighterStyle.TwoWeapon?Text("style.two"):CharacterDescriptionText.Get(d.fightingStyle==FighterStyle.Archery?"style.archery":d.fightingStyle==FighterStyle.Dueling?"style.dueling":"style.great.14"));
            if(d.level>=6)Add("base.brand",Math.Max(1,HemocraftModifier(d))*(d.level>=13?2:1),Math.Min(9,d.level/2));
            if(d.level>=9)Add("base.psychometry");
            if(d.level>=10)Add("base.augmentation",Math.Max(1,HemocraftModifier(d)));
            if(d.level>=13)Add("base.tether"); if(d.level>=14)Add("base.soul"); if(d.level>=20)Add("base.master");
            if(Is(d,"ghostslayer")) {
                Add("ghost.dawn"); if(d.level>=7)Add("ghost.step",Math.Max(1,HemocraftModifier(d)));
                if(d.level>=11)Add("ghost.brand");if(d.level>=15)Add("curse.exorcist");if(d.level>=18)Add("ghost.revive");
            }
            if(Is(d,"mutant")) {
                Add("mutant.craft",Mutagens(d)); foreach(string id in d.bloodFormulas.Where(s=>!string.IsNullOrEmpty(s)))lines.Add(FormulaDescription(id));
                if(d.level>=7)Add("mutant.metabolism");if(d.level>=11)Add("mutant.brand");if(d.level>=15)Add("curse.corrosion");if(d.level>=18)Add("mutant.exalted");
            }
            if(Is(d,"lycan")) {
                int bonus=d.level>=18?3:d.level>=11?2:1;
                Add("lycan.senses");Add("lycan.hybrid",bonus);Add("lycan.strikes",d.level>=11?"1d8":"1d6");Add("lycan.bloodlust");
                if(d.level>=7)Add("lycan.stalker",bonus);if(d.level>=11)Add("lycan.regen",Math.Max(1,1+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2])));
                if(d.level>=15)Add("lycan.brand");if(d.level>=18){Add("lycan.master");Add("curse.howl");}
            }
            if(Profane(d)) {
                Add("profane.focus");if(Patrons.Contains(d.bloodPatron))Add("focus."+d.bloodPatron);
                if(d.level>=7)Add("profane.frenzy");if(d.level>=11)Add("profane.brand");if(d.level>=18)Add("curse.souleater");
                // Spell details and the per-rest restrictions are written by presentation in the selected language.
            }
            return string.Join("\n",lines)+"\n";
        }
    }
}
