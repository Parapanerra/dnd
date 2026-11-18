using System;
using System.Collections.Generic;
using System.Linq;

namespace Taruck.CharacterCreation
{
    public static class CharacterWarlockRules
    {
        public static readonly string[] Patrons = { "archfey", "fiend", "greatoldone", "celestial" };
        public static readonly string[] PatronNames = { "Архіфея", "Потойбічне породження", "Великий Давній", "Небесний покровитель" };
        public static readonly string[] PactWeapons = { "Дубина", "Кинджал", "Велика дубина", "Ручна сокира", "Метальний спис", "Легкий молот", "Булава", "Бойовий посох", "Серп", "Спис", "Бойова сокира", "Ціп", "Глефа", "Сокира дворучна", "Дворучний меч", "Алебарда", "Кавалерійський спис", "Довгий меч", "Бойовий молот дворучний", "Моргенштерн", "Піка", "Рапіра", "Скімітар", "Короткий меч", "Тризуб", "Бойова кірка", "Бойовий молот", "Батіг" };
        public static readonly string[] PactDamage = { "1d4", "1d4", "1d8", "1d6", "1d6", "1d4", "1d6", "1d6", "1d4", "1d6", "1d8", "1d8", "1d10", "1d12", "2d6", "1d10", "1d10", "1d8", "2d6", "1d8", "1d10", "1d8", "1d6", "1d6", "1d8", "1d8", "1d8", "1d4" };
        public static readonly string[] ResistanceTypes=CharacterLevelAdvancement.DamageTypes.Concat(new[]{"Дробильна","Колюча","Рубана","Силова"}).ToArray();
        public static bool Active(CharacterCreationDraft d)=>d.characterClass=="warlock";
        public static bool Modern(CharacterCreationDraft d)=>Active(d)&&d.edition==RulesEdition.Rules2024;
        public static bool Is(CharacterCreationDraft d,string patron)=>Active(d)&&d.level>=(Modern(d)?3:1)&&d.subclass==patron;
        public static string[] PatronOptions(CharacterCreationDraft d)=>Modern(d)?Patrons:Patrons.Take(3).ToArray();
        public static bool Has(CharacterCreationDraft d,string id)=>Active(d)&&(d.warlockInvocations??new List<string>()).Any(key=>key==id||key.StartsWith(id+":",StringComparison.Ordinal));
        public static bool Blade(CharacterCreationDraft d)=>Active(d)&&(Modern(d)?Has(d,"blade"):d.level>=3&&d.warlockPact=="blade");
        public static bool Chain(CharacterCreationDraft d)=>Active(d)&&(Modern(d)?Has(d,"chain"):d.level>=3&&d.warlockPact=="chain");
        public static bool Tome(CharacterCreationDraft d)=>Active(d)&&(Modern(d)?Has(d,"tome"):d.level>=3&&d.warlockPact=="tome");
        public static int SlotCircle(CharacterCreationDraft d)=>Active(d)?Math.Min(5,(d.level+1)/2):0;
        public static int SlotCount(CharacterCreationDraft d)=>!Active(d)?0:d.level>=17?4:d.level>=11?3:d.level>=2?2:1;
        internal static int Cantrips(CharacterCreationDraft d)=>!Active(d)?0:d.level>=10?4:d.level>=4?3:2;
        public static readonly int[] Known={2,3,4,5,6,7,8,9,10,10,11,11,12,12,13,13,14,14,15,15};
        static readonly int[] Inv14={0,2,2,2,3,3,4,4,5,5,5,6,6,6,7,7,7,8,8,8};
        static readonly int[] Inv24={1,3,3,3,5,5,6,6,7,7,7,8,8,8,9,9,9,10,10,10};
        internal static int SpellCount(CharacterCreationDraft d)=>Active(d)?Known[Math.Clamp(d.level,1,20)-1]:0;
        public static int InvocationCount(CharacterCreationDraft d)=>Active(d)?(Modern(d)?Inv24:Inv14)[Math.Clamp(d.level,1,20)-1]:0;
        public static int ArcanumCount(CharacterCreationDraft d)=>!Active(d)||d.level<11?0:Math.Min(4,(d.level-9)/2);
        public static string Text(string id)=>CharacterWarlockText.Get(id);
        public static bool CanLearn(CharacterCreationDraft d,IEnumerable<int> circles)
        {
            var values=circles.ToArray();if(values.Any(c=>c<1||c>SlotCircle(d)))return false;
            for(int c=2;c<=SlotCircle(d);c++){int first=c*2-1;int capacity=SpellCount(d)-Known[first-2]+d.level-first+1;if(values.Count(v=>v>=c)>capacity)return false;}
            return true;
        }
        public static string[] ExpandedSpells(CharacterCreationDraft d)
        {
            if(!Active(d)||Modern(d))return Array.Empty<string>();
            string[] names=d.subclass=="archfey"?new[]{"Faerie Fire","Sleep","Calm Emotions","Phantasmal Force","Blink","Plant Growth","Dominate Beast","Greater Invisibility","Dominate Person","Seeming"}:d.subclass=="fiend"?new[]{"Burning Hands","Command","Blindness/Deafness","Scorching Ray","Fireball","Stinking Cloud","Fire Shield","Wall of Fire","Flame Strike","Hallow"}:d.subclass=="greatoldone"?new[]{"Dissonant Whispers","Tasha's Hideous Laughter","Detect Thoughts","Phantasmal Force","Clairvoyance","Sending","Dominate Beast","Evard's Black Tentacles","Dominate Person","Telekinesis"}:Array.Empty<string>();
            return names.Take(SlotCircle(d)*2).ToArray();
        }
        public static string[] AutomaticSpells(CharacterCreationDraft d)
        {
            if(!Modern(d))return Array.Empty<string>();var names=new List<string>();
            if(d.level>=3){string[] all=d.subclass=="archfey"?new[]{"Calm Emotions","Faerie Fire","Phantasmal Force","Sleep","Blink","Plant Growth","Dominate Beast","Greater Invisibility","Dominate Person","Seeming"}:d.subclass=="celestial"?new[]{"Aid","Cure Wounds","Guiding Bolt","Lesser Restoration","Daylight","Revivify","Guardian of Faith","Wall of Fire","Greater Restoration","Summon Celestial"}:d.subclass=="fiend"?new[]{"Burning Hands","Command","Scorching Ray","Suggestion","Fireball","Stinking Cloud","Fire Shield","Wall of Fire","Geas","Insect Plague"}:d.subclass=="greatoldone"?new[]{"Detect Thoughts","Dissonant Whispers","Phantasmal Force","Tasha's Hideous Laughter","Clairvoyance","Hunger of Hadar","Confusion","Summon Aberration","Modify Memory","Telekinesis"}:Array.Empty<string>();names.AddRange(all.Take(4+2*((Math.Min(9,d.level)-3)/2)));
                if(d.subclass=="archfey")names.Add("Misty Step");if(d.subclass=="celestial")names.AddRange(new[]{"Light","Sacred Flame"});if(d.subclass=="greatoldone"&&d.level>=10)names.Add("Hex");}
            if(d.level>=9)names.Add("Contact Other Plane");return names.Distinct().ToArray();
        }
        internal static void Repair(CharacterCreationDraft d)
        {
            d.warlockInvocations??=new List<string>();d.warlockArcana??=new List<string>();d.warlockLessons??=new List<CharacterOriginFeatChoice>();d.tomeCantrips??=new List<string>();d.tomeRituals??=new List<string>();
            if(!Active(d)){d.warlockInvocations.Clear();d.warlockArcana.Clear();d.warlockLessons.Clear();d.invocation="";d.warlockPact="";return;}
            if(Modern(d)&&d.warlockInvocations.Count==0&&!string.IsNullOrEmpty(d.invocation))d.warlockInvocations.Add(d.invocation);
            if(d.warlockInvocations.Count>InvocationCount(d))d.warlockInvocations.RemoveRange(InvocationCount(d),d.warlockInvocations.Count-InvocationCount(d));
            while(d.warlockInvocations.Count<InvocationCount(d))d.warlockInvocations.Add("");
            // Changing a prerequisite clears its dependents too, regardless of picker order.
            for(int pass=0;pass<d.warlockInvocations.Count+1;pass++){bool changed=false;var seen=new HashSet<string>();for(int i=0;i<d.warlockInvocations.Count;i++){string key=d.warlockInvocations[i];if(key.Length>0&&(!CharacterWarlockInvocations.Allowed(d,key)||!seen.Add(key))){d.warlockInvocations[i]="";changed=true;}}if(!changed)break;}
            d.invocation=Modern(d)?d.warlockInvocations.FirstOrDefault()??"":"";
            if(Modern(d)||d.level<3)d.warlockPact="";
            if(!Tome(d)){d.tomeCantrips.Clear();d.tomeRituals.Clear();}else if(!Modern(d)&&!Has(d,"ancient"))d.tomeRituals.Clear();
            if(!Blade(d))d.pactWeapon=-1;
            while(d.warlockArcana.Count<ArcanumCount(d))d.warlockArcana.Add("");if(d.warlockArcana.Count>ArcanumCount(d))d.warlockArcana.RemoveRange(ArcanumCount(d),d.warlockArcana.Count-ArcanumCount(d));
            string[] feats=d.warlockInvocations.Where(k=>k.StartsWith("lessons:",StringComparison.Ordinal)).Select(k=>k.Substring(8)).ToArray();
            d.warlockLessons.RemoveAll(f=>f==null||!feats.Contains(f.id));foreach(string id in feats)if(!d.warlockLessons.Any(f=>f.id==id))d.warlockLessons.Add(new CharacterOriginFeatChoice{id=id});
            foreach(string id in AutomaticSpells(d))for(int i=0;i<d.preparedSpells.Count;i++)if(d.preparedSpells[i]==id)d.preparedSpells[i]="";
        }
        public static string ChoiceError(CharacterCreationDraft d,int page)
        {
            if(!Active(d))return null;
            if(page==0){if(d.level>=(Modern(d)?3:1)&&!PatronOptions(d).Contains(d.subclass))return Text("error.patron");if(!Modern(d)&&d.level>=3&&!new[]{"blade","chain","tome"}.Contains(d.warlockPact))return Text("error.pact");}
            if(page!=4)return null;
            if(d.warlockInvocations.Count!=InvocationCount(d)||d.warlockInvocations.Any(k=>!CharacterWarlockInvocations.Allowed(d,k))||d.warlockInvocations.Distinct().Count()!=InvocationCount(d))return Text("error.invocations");
            if(!d.IsSecondaryTrack&&!Modern(d)&&(!CharacterCreationRules.MonkSimpleWeapons.Contains(d.warlockWeapon)||!CharacterCreationRules.MonkSimpleWeapons.Contains(d.warlockSecondWeapon)||!new[]{"Сумка компонентів","Арканний фокус"}.Contains(d.warlockFocus)||d.warlockPack<0||d.warlockPack>1))return "Обери зброю, фокус і набір чаклуна.";
            if(Blade(d)&&(d.pactWeapon<0||d.pactWeapon>=PactWeapons.Length))return "Обери зброю договору клинка.";
            int rituals=Modern(d)||Has(d,"ancient")?2:0;
            if(Tome(d)&&(d.tomeCantrips.Count!=3||d.tomeCantrips.Any(string.IsNullOrEmpty)||d.tomeCantrips.Distinct().Count()!=3||d.tomeCantrips.Any(d.cantrips.Contains)||d.tomeRituals.Count!=rituals||d.tomeRituals.Any(string.IsNullOrEmpty)||d.tomeRituals.Distinct().Count()!=rituals||d.tomeRituals.Any(d.preparedSpells.Contains)))return "Обери три додаткові заговори й два ритуали 1 рівня без повторів.";
            if(d.warlockArcana.Count!=ArcanumCount(d)||d.warlockArcana.Any(string.IsNullOrEmpty))return Text("error.arcana");
            if(Is(d,"fiend")&&d.level>=10&&(d.warlockResistance<0||d.warlockResistance>=ResistanceTypes.Length||Modern(d)&&ResistanceTypes[d.warlockResistance]=="Силова"))return Text("error.resistance");
            return null;
        }
        public static int PactModifier(CharacterCreationDraft d,int[] scores)=>CharacterCreationRules.Modifier(Modern(d)?scores[5]:new[]{"Кинджал","Рапіра","Скімітар","Короткий меч","Батіг"}.Contains(d.pactWeapon>=0&&d.pactWeapon<PactWeapons.Length?PactWeapons[d.pactWeapon]:"")?Math.Max(scores[0],scores[1]):scores[0]);
        internal static CreatedCharacterStats Calculate(CharacterCreationDraft d)
        {
            int[] scores=CharacterCreationRules.Scores(d);int Mod(int a)=>CharacterCreationRules.Modifier(scores[a]);int pb=CharacterLegacySpeciesRules.Proficiency(d);var skills=CharacterCreationRules.BackgroundSkills(d).Concat(d.classSkills).ToList();
            if(Has(d,"beguiling"))skills.AddRange(new[]{4,13});
            var result=new CreatedCharacterStats{scores=scores,skills=skills.Distinct().ToList(),hitPoints=CharacterCreationRules.ClassHitPoints(d,Mod(2)),armorClass=(Modern(d)&&d.equipment==1?10:11)+Mod(1),initiative=Mod(1),speed=CharacterSpeciesRules.IsDwarf(d)&&!Modern(d)?25:30,passivePerception=10+Mod(4)+(skills.Contains(11)?pb:0),speciesName=CharacterSpeciesRules.Name(d),backgroundName=CharacterBackgroundRules.Find(d)?.name??"",equipment=Equipment(d)};
            result.features=Features(d)+CharacterBackgroundRules.Features(d);return result;
        }
        internal static string Features(CharacterCreationDraft d)
        {
            var parts=new List<string>{string.Format(Text("pactmagic"),SlotCount(d),SlotCircle(d))};
            if(Modern(d)&&d.level>=2)parts.Add(string.Format(Text("cunning"),d.level>=20?SlotCount(d):(SlotCount(d)+1)/2));
            if(!Modern(d)&&d.level>=20)parts.Add(Text("master14"));if(Modern(d)&&d.level>=9)parts.Add(Text("contact"));
            if(!Modern(d)&&d.level>=3&&new[]{"blade","chain","tome"}.Contains(d.warlockPact))parts.Add(Text("pact."+d.warlockPact));
            if(ArcanumCount(d)>0)parts.Add(Text("arcana"));
            if(d.level>=(Modern(d)?3:1)&&PatronOptions(d).Contains(d.subclass))foreach(int lv in new[]{Modern(d)?3:1,6,10,14}.Where(l=>l<=d.level))parts.Add(Text(d.subclass+"."+lv+"."+(Modern(d)?"24":"14")));
            if(Is(d,"fiend")&&d.level>=10&&d.warlockResistance>=0&&d.warlockResistance<ResistanceTypes.Length)parts.Add(string.Format(Text("resistance"),ResistanceTypes[d.warlockResistance]));
            return string.Join("\n\n",parts);
        }
        internal static string Equipment(CharacterCreationDraft d) => Modern(d)
            ? d.equipment == 0 ? "Шкіряний обладунок; серп; 2 кинджали; арканний фокус (сфера); книга окультних знань; набір ученого; 15 зм." : "100 зм замість класового спорядження."
            : d.warlockWeapon + (d.warlockWeapon == "Легкий арбалет" ? "; 20 болтів" : "") + "; " + d.warlockSecondWeapon + "; " + d.warlockFocus + "; шкіряний обладунок; 2 кинджали; " + (d.warlockPack == 0 ? "набір ученого." : "набір підземельника.");
    }
}
