using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterBardRules
    {
        public static readonly string[] Modern = { "lore", "valor", "dance", "glamour" };
        public static readonly string[] Names = { "Колегія знань", "Колегія доблесті", "Колегія танцю", "Колегія чарівності" };
        private static readonly int[] ClassicSpells = {4,5,6,7,8,9,10,11,12,14,15,15,16,18,19,19,20,22,22,22};
        private static readonly int[] ModernSpells = {4,5,6,7,9,10,11,12,14,15,16,16,17,17,18,18,19,20,21,22};
        public static bool Active(CharacterCreationDraft d) => d.characterClass == "bard";
        public static bool Revised(CharacterCreationDraft d) => d.edition == RulesEdition.Rules2024;
        public static string[] Options(CharacterCreationDraft d) => Revised(d) ? Modern : Modern.Take(2).ToArray();
        public static bool Is(CharacterCreationDraft d, string id) => Active(d) && d.level >= 3 && d.subclass == id && Options(d).Contains(id);
        public static int Die(CharacterCreationDraft d) => d.level >= 15 ? 12 : d.level >= 10 ? 10 : d.level >= 5 ? 8 : 6;
        internal static int SongDie(CharacterCreationDraft d) => d.level >= 17 ? 12 : d.level >= 13 ? 10 : d.level >= 9 ? 8 : 6;
        public static int Cantrips(CharacterCreationDraft d) => !Active(d) || d.level < 1 ? 0 : d.level >= 10 ? 4 : d.level >= 4 ? 3 : 2;
        public static int MaximumCircle(CharacterCreationDraft d) => !Active(d) || d.level < 1 ? 0 : Math.Min(9,(d.level+1)/2);
        internal static int Slots(CharacterCreationDraft d, int circle) => Active(d) ? CharacterFullCasterSlots.Count(d.level,circle) : 0;
        public static int CoreSecrets(CharacterCreationDraft d) => !Active(d) || Revised(d) ? 0 : (d.level >= 10 ? 2 : 0) + (d.level >= 14 ? 2 : 0) + (d.level >= 18 ? 2 : 0);
        internal static int LoreSecrets(CharacterCreationDraft d) => Is(d,"lore") && d.level >= 6 ? 2 : 0;
        public static int SecretCount(CharacterCreationDraft d) => CoreSecrets(d) + LoreSecrets(d);
        public static int SpellCount(CharacterCreationDraft d) => !Active(d) || d.level < 1 ? 0 : (Revised(d) ? ModernSpells : ClassicSpells)[Math.Min(20,d.level)-1] - CoreSecrets(d);
        public static int SecretCircle(CharacterCreationDraft d, int slot) => slot < LoreSecrets(d) ? Revised(d) ? MaximumCircle(d) : 3 : new[]{5,5,7,7,9,9}[slot-LoreSecrets(d)];
        public static int ExpertiseCount(CharacterCreationDraft d) => !Active(d) ? 0 : (d.level >= (Revised(d)?2:3) ? 2 : 0) + (d.level >= (Revised(d)?9:10) ? 2 : 0);
        public static int LoreSkillCount(CharacterCreationDraft d) => Is(d,"lore") ? 3 : 0;
        internal static int HalfProficiency(CharacterCreationDraft d) => Active(d) && d.level >= 2 ? CharacterLegacySpeciesRules.Proficiency(d)/2 : 0;
        public static IEnumerable<int> ExtraSkills(CharacterCreationDraft d) => Is(d,"lore") && d.bardLoreSkills != null ? d.bardLoreSkills.Where(x=>x>=0&&x<18) : Enumerable.Empty<int>();
        public static string[] AutomaticSpells(CharacterCreationDraft d) {
            var result=new List<string>();
            if(Is(d,"glamour")) { result.AddRange(new[]{"Charm Person","Mirror Image"}); if(d.level>=6)result.Add("Command"); }
            if(Active(d)&&Revised(d)&&d.level>=20)result.AddRange(new[]{"Power Word Heal","Power Word Kill"});
            return result.ToArray();
        }
        internal static void Repair(CharacterCreationDraft d) {
            if(d.bardLoreSkills==null)d.bardLoreSkills=new List<int>();
            if(d.bardSecrets==null)d.bardSecrets=new List<string>();
            var granted=CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Concat(d.species=="human"&&Revised(d)&&d.humanSkill>=0?new[]{d.humanSkill}:Array.Empty<int>()).ToArray();
            var seen=new HashSet<int>();
            for(int i=0;i<d.bardLoreSkills.Count;i++)if(!Is(d,"lore")||d.bardLoreSkills[i]<0||d.bardLoreSkills[i]>=18||granted.Contains(d.bardLoreSkills[i])||!seen.Add(d.bardLoreSkills[i]))d.bardLoreSkills[i]=-1;
            if(d.bardLoreSkills.Count>LoreSkillCount(d))d.bardLoreSkills.RemoveRange(LoreSkillCount(d),d.bardLoreSkills.Count-LoreSkillCount(d));
            if(d.bardSecrets.Count>SecretCount(d))d.bardSecrets.RemoveRange(SecretCount(d),d.bardSecrets.Count-SecretCount(d));
            var names=new HashSet<string>(d.cantrips.Concat(d.preparedSpells).Concat(AutomaticSpells(d)));
            for(int i=0;i<d.bardSecrets.Count;i++)if(!string.IsNullOrEmpty(d.bardSecrets[i])&&!names.Add(d.bardSecrets[i]))d.bardSecrets[i]="";
            if(Active(d)) {
                if(d.expertise.Count>ExpertiseCount(d))d.expertise.RemoveRange(ExpertiseCount(d),d.expertise.Count-ExpertiseCount(d));
                var automatic=AutomaticSpells(d);
                for(int i=0;i<d.preparedSpells.Count;i++)if(automatic.Contains(d.preparedSpells[i]))d.preparedSpells[i]="";
            }
        }
        // A Bard gains only the table increase plus one replacement at each level.
        public static bool CanLearn(CharacterCreationDraft d, IEnumerable<KeyValuePair<int,bool>> choices) {
            var picked=choices.ToArray();if(!Active(d))return true;
            int level=Math.Min(20,d.level),max=MaximumCircle(d);
            if(level<1)return picked.Length==0;
            if(picked.Any(x=>x.Key<1||x.Key>max))return false;
            for(int circle=2;circle<=max;circle++) {
                int unlock=circle*2-1;
                int before=(Revised(d)?ModernSpells:ClassicSpells)[unlock-2];
                int total=Revised(d)?SpellCount(d):ClassicSpells[level-1]+LoreSecrets(d);
                if(!Revised(d)&&unlock-1>=6)before+=LoreSecrets(d);
                if(picked.Count(x=>x.Key>=circle)>total-before+level-unlock+1)return false;
            }
            if(Revised(d))for(int circle=1;circle<=max;circle++) {
                int unlock=Math.Max(10,circle*2-1);
                int count=picked.Count(x=>!x.Value&&x.Key>=circle);
                if(level<unlock) { if(count>0)return false; }
                else if(count>ModernSpells[level-1]-ModernSpells[unlock-2]+level-unlock+1)return false;
            }
            return true;
        }
        public static string ChoiceError(CharacterCreationDraft d, int page) {
            if(!Active(d))return null;
            if(page==0&&d.level>=3&&!Options(d).Contains(d.subclass))return CharacterBardText.Get("error.subclass");
            if(page==3) {
                if(d.bardLoreSkills.Count!=LoreSkillCount(d)||d.bardLoreSkills.Any(x=>x<0)||d.bardLoreSkills.Distinct().Count()!=LoreSkillCount(d))return CharacterBardText.Get("error.skills");
                if(d.expertise.Count!=ExpertiseCount(d)||d.expertise.Distinct().Count()!=ExpertiseCount(d)||d.expertise.Any(x=>x<0||!CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(CharacterOriginFeatRules.Skills(d)).Concat(ExtraSkills(d)).Concat(d.species=="human"&&Revised(d)?new[]{d.humanSkill}:Array.Empty<int>()).Contains(x)))return CharacterBardText.Get("error.expertise");
            }
            if(page==4&&(d.bardSecrets.Count!=SecretCount(d)||d.bardSecrets.Any(string.IsNullOrEmpty)))return CharacterBardText.Get("error.secrets");
            return null;
        }
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d) {
            int[] s=CharacterCreationRules.Scores(d);int pb=CharacterLegacySpeciesRules.Proficiency(d);
            var skills=CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).Concat(CharacterSpeciesRules.GrantedSkills(d)).Concat(ExtraSkills(d)).Concat(Revised(d)&&d.species=="human"&&d.humanSkill>=0?new[]{d.humanSkill}:Array.Empty<int>()).Distinct().ToList();
            bool modern=Revised(d);int dex=CharacterCreationRules.Modifier(s[1]);
            int ac=(modern&&d.equipment==1?10:11)+dex;
            // Dance can keep its starting leather or choose the better unarmored formula.
            if(Is(d,"dance"))ac=Math.Max(ac,10+dex+CharacterCreationRules.Modifier(s[5]));
            var result=new CreatedCharacterStats {scores=s,skills=skills,expertise=d.expertise.ToList(),hitPoints=CharacterCreationRules.ClassHitPoints(d,CharacterCreationRules.Modifier(s[2])),armorClass=ac,
                initiative=dex+(!modern?HalfProficiency(d):0),speed=30,
                passivePerception=10+CharacterCreationRules.Modifier(s[4])+(d.expertise.Contains(11)?2*pb:skills.Contains(11)?pb:HalfProficiency(d)),
                speciesName=CharacterSpeciesRules.IsDwarf(d)?modern?"Дварф":"Дварф (пагорбовий)":"Людина",backgroundName=CharacterBackgroundRules.Find(d)?.name??""};
            result.equipment=CharacterCreationRules.Equipment(d);result.features=Features(d);return result;
        }
        internal static string Features(CharacterCreationDraft d) {
            bool modern=Revised(d);string v=modern?"24":"14";
            var ids=new List<string>{"inspiration"+v};
            if(d.level>=2)ids.Add("jack"+v);
            if(!modern&&d.level>=2)ids.Add("song");
            if(d.level>=5)ids.Add("font"+v);
            if(d.level>=(modern?7:6))ids.Add("counter"+v);
            if(d.level>=(modern?18:20))ids.Add("superior"+v);
            if(modern&&d.level>=20)ids.Add("creation");
            if(Is(d,"lore")) { ids.Add("lore.cut"+v);if(d.level>=14)ids.Add("lore.peerless"+v); }
            if(Is(d,"valor")) { ids.Add("valor.3."+v);if(d.level>=6)ids.Add("valor.6."+v);if(d.level>=14)ids.Add("valor.14."+v); }
            if(Is(d,"dance"))foreach(int n in new[]{3,6,14})if(d.level>=n)ids.Add("dance."+n);
            if(Is(d,"glamour"))foreach(int n in new[]{3,6,14})if(d.level>=n)ids.Add("glamour."+n);
            var lines=ids.Select(CharacterBardText.Get).ToList();
            if(d.level>=10)lines.Add(CharacterDescriptionText.Get(modern?"bard.secrets24":"bard.secrets14"));
            if(Is(d,"lore"))
            {
                lines.Add(CharacterDescriptionText.Get("bard.lore.skills"));
                if(d.level>=6)lines.Add(CharacterDescriptionText.Get(modern?"bard.lore.discoveries":"bard.loresecrets"));
            }
            if(CharacterSpeciesRules.IsDwarf(d))lines.Add(modern?CharacterFighterText.Get("dwarf24"):CharacterSpeciesRules.Dwarf2014Features(d).TrimStart('\n'));
            if(modern&&d.species=="human")lines.Add("Людина: героїчне натхнення після довгого відпочинку; додаткова навичка.");
            return string.Join("\n",lines)+CharacterBackgroundRules.Features(d);
        }
    }
}
