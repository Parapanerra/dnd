using System;
using System.Linq;

namespace Taruck.CharacterCreation
{
    // Published sources: MPMM, VRGR, AAG and ERLW/EFA. No invented 2024 reprints.
    public static class CharacterExpandedSpeciesRules
    {
        public static readonly string[] Ids={"tabaxi","kenku","goblin","kalashtar","dhampir","astralelf","bugbear","hobgoblin","changeling","harengon","owlin","leonin","aarakocra","fairy","firbolg","giff","hadozee","plasmoid","centaur","minotaur","satyr","seaelf","kender","verdan","deepgnome","duergar","githyanki","githzerai","hexblood","reborn","triton","yuanti","genasi","eladrin","shadarkai","kobold","shifter","lizardfolk","tortle","locathah","loxodon","autognome","thrikreen","vedalken","simic","dragonbornftd","khoravar","orclegacy","aasimarlegacy","goliathlegacy","lupin","lorwynchangeling","rimekin"};
        internal static bool RevisedOnly(string id)=>new[]{"khoravar","lupin","lorwynchangeling","rimekin"}.Contains(id);
        public static bool Active(CharacterCreationDraft d)=>Ids.Contains(d.species);
        public static string Text(string key)=>CharacterExpandedSpeciesText.Get(key);
        internal static string Name(string id)=>Ids.Contains(id)?Text("name."+id):"";
        public static bool Flexible(CharacterCreationDraft d)=>Active(d)&&d.species!="kalashtar"&&d.species!="leonin"&&d.species!="verdan"&&d.species!="locathah"&&d.species!="loxodon"&&d.species!="vedalken"&&d.species!="simic"&&d.edition==RulesEdition.Rules2014;
        internal static bool NeedsSize(CharacterCreationDraft d)=>d.species=="shifter"&&d.edition==RulesEdition.Rules2024||new[]{"tabaxi","kenku","dhampir","changeling","harengon","owlin","hadozee","plasmoid","hexblood","reborn","yuanti","genasi","tortle","thrikreen","khoravar","aasimarlegacy","lupin","lorwynchangeling","rimekin"}.Contains(d.species);
        public static string Source(CharacterCreationDraft d)=>d.species=="lupin"?"RHW":d.species=="lorwynchangeling"||d.species=="rimekin"?"LFL":d.species=="khoravar"?"EFA":CharacterFtdDragonbornRules.Active(d)?"FTD":d.species=="loxodon"||d.species=="vedalken"||d.species=="simic"?"GGR":d.species=="locathah"?"LR":d.species=="shifter"&&d.edition==RulesEdition.Rules2024?"EFA":d.species=="kender"?"DSotDQ":d.species=="verdan"?"AI":d.species=="leonin"?"MOT":d.species=="owlin"?"SCC":d.species=="kalashtar"?d.edition==RulesEdition.Rules2024?"EFA":"ERLW":d.species=="changeling"&&d.edition==RulesEdition.Rules2024?"EFA":new[]{"astralelf","giff","hadozee","plasmoid","autognome","thrikreen"}.Contains(d.species)?"AAG":new[]{"dhampir","hexblood","reborn"}.Contains(d.species)?"VRGR":"MPMM";
        public static int SkillCount(CharacterCreationDraft d)=>new[]{"kenku","dhampir","changeling","hexblood","reborn","lizardfolk"}.Contains(d.species)?2:d.species=="lupin"||d.species=="lorwynchangeling"||d.species=="khoravar"&&d.expandedGift=="skill"||d.species=="vedalken"||d.species=="tortle"||d.species=="shifter"||d.species=="kobold"&&d.expandedGift=="craft"||d.species=="kender"||d.species=="centaur"||d.species=="leonin"||d.species=="astralelf"||d.species=="githyanki"||d.species=="kalashtar"&&d.edition==RulesEdition.Rules2024?1:0;
        internal static int[] Skills(CharacterCreationDraft d)=>(d.species=="goliathlegacy"?new[]{3}:d.species=="locathah"?new[]{3,11}:d.species=="verdan"?new[]{13}:d.species=="satyr"?new[]{12,13}:d.species=="tabaxi"?new[]{11,16}:d.species=="bugbear"||d.species=="owlin"?new[]{16}:d.species=="astralelf"||d.species=="seaelf"||d.species=="harengon"||d.species=="eladrin"||d.species=="shadarkai"?new[]{11}:Array.Empty<int>()).Concat((d.expandedSkills??Array.Empty<int>()).Take(SkillCount(d)).Where(i=>i>=0&&i<18)).Distinct().ToArray();
        public static int[] SkillOptions(CharacterCreationDraft d,int slot)=>(d.species=="lupin"?new[]{11,16,17}:d.species=="lorwynchangeling"?new[]{4,12}:d.species=="vedalken"?new[]{2,5,8,9,12,15}:d.species=="lizardfolk"||d.species=="tortle"?new[]{1,9,10,11,16,17}:d.species=="shifter"?new[]{0,3,7,17}:d.species=="kobold"?new[]{2,8,9,15,17}:d.species=="kender"?new[]{6,8,15,16,17}:d.species=="centaur"?new[]{1,9,10,17}:d.species=="changeling"?new[]{4,6,7,12,13}:d.species=="leonin"?new[]{3,7,11,17}:Enumerable.Range(0,18)).Where(i=>
            !CharacterCreationRules.OriginalBackgroundSkills(d).Contains(i)&&!d.classSkills.Contains(i)&&!CharacterOriginFeatRules.Skills(d).Contains(i)
            &&(d.species!="astralelf"||i!=11)&&!d.expandedSkills.Where((value,index)=>index!=slot).Contains(i)).ToArray();
        public static string[] TranceOptions()=>CharacterWeaponCatalog.All.Where(w=>w.edition==RulesEdition.Rules2014).Select(w=>"weapon:"+w.name).Concat(CharacterBackgroundRules.AllTools.Select(t=>"tool:"+t)).ToArray();
        public static int TranceCount(CharacterCreationDraft d)=>d.species=="astralelf"||d.species=="githyanki"?1:new[]{"seaelf","eladrin","shadarkai"}.Contains(d.species)?2:0;
        public static string Trance(CharacterCreationDraft d,int slot)=>slot==0?d.expandedTranceProficiency:d.expandedSecondTranceProficiency;
        public static string[] TranceOptions(CharacterCreationDraft d,int slot)=>TranceOptions().Where(key=>TranceCount(d)==1||
            !Enumerable.Range(0,TranceCount(d)).Where(i=>i!=slot).Select(i=>Trance(d,i)).Contains(key)&&
            (key.StartsWith("weapon:",StringComparison.Ordinal)?!CharacterWeaponProficiency.Has(d,CharacterWeaponCatalog.Find(key.Substring(7),RulesEdition.Rules2014),false):
            !CharacterBackgroundRules.ClassTools(d,false).Concat(CharacterBackgroundRules.Tools(d)).Concat(CharacterOriginFeatRules.Tools(d)).Contains(key.Substring(5)))).ToArray();
        public static int ToolCount(CharacterCreationDraft d)=>d.species=="autognome"?2:d.species=="vedalken"||d.species=="khoravar"&&d.expandedGift=="tool"?1:0;
        internal static string[] PermanentTools(CharacterCreationDraft d)=>ToolCount(d)>0?(d.expandedTools??Array.Empty<string>()).Take(ToolCount(d)).Where(CharacterBackgroundRules.AllTools.Contains).Distinct().ToArray():Array.Empty<string>();
        public static string[] ToolOptions(CharacterCreationDraft d,int slot)=>CharacterBackgroundRules.AllTools.Except(CharacterBackgroundRules.ClassTools(d,false)).Except(CharacterBackgroundRules.Find(d)?.tools??Array.Empty<string>()).Except(new[]{CharacterBackgroundRules.ToolChoice(d)}).Except(d.backgroundToolReplacements??Array.Empty<string>()).Except(CharacterOriginFeatRules.Tools(d)).Where(tool=>!(d.expandedTools??Array.Empty<string>()).Where((value,index)=>index!=slot).Contains(tool)).ToArray();
        public static readonly string[] SimicFirst={"manta","climber","underwater"};
        public static readonly string[] SimicAll={"manta","climber","underwater","appendages","carapace","acid"};
        public static string[] SimicOptions(CharacterCreationDraft d,int slot)=>(slot==0?SimicFirst:SimicAll).Where(value=>slot==0||value!=d.simicEnhancements[0]).ToArray();
        public static bool Simic(CharacterCreationDraft d,string feature)=>d.species=="simic"&&(d.simicEnhancements??Array.Empty<string>()).Take(d.TotalLevel>=5?2:1).Contains(feature);
        public static string[] InstrumentOptions(CharacterCreationDraft d)=>CharacterCreationRules.MusicalInstruments.Except(CharacterBackgroundRules.ClassTools(d,false)).Except(CharacterBackgroundRules.Tools(d)).Except(CharacterOriginFeatRules.Tools(d)).ToArray();
        public static bool UsesMagic(CharacterCreationDraft d)=>d.species=="rimekin"||d.species=="khoravar"||d.species=="kobold"&&d.expandedGift=="magic"||new[]{"astralelf","aarakocra","fairy","firbolg","deepgnome","duergar","githyanki","githzerai","hexblood","triton","yuanti","genasi"}.Contains(d.species);
        public static string[] FreeSpells(CharacterCreationDraft d)
        {
            if(d.species=="rimekin")return new[]{d.TotalLevel>=3?"Ice Knife":"",d.TotalLevel>=5?"Flame Blade":""}.Where(name=>name.Length>0).ToArray();
            if(d.species=="genasi")
            {
                string a=d.speciesLineage=="air"?"Feather Fall":d.speciesLineage=="fire"?"Burning Hands":d.speciesLineage=="water"?"Create or Destroy Water":"";
                string b=d.speciesLineage=="air"?"Levitate":d.speciesLineage=="fire"?"Flame Blade":d.speciesLineage=="water"?"Water Walk":d.speciesLineage=="earth"?"Pass without Trace":"";
                return new[]{d.TotalLevel>=3?a:"",d.TotalLevel>=5?b:""}.Where(name=>name.Length>0).ToArray();
            }
            if(d.species=="aarakocra")return d.TotalLevel>=3?new[]{"Gust of Wind"}:Array.Empty<string>();
            if(d.species=="triton")return new[]{"Fog Cloud",d.TotalLevel>=3?"Gust of Wind":"",d.TotalLevel>=5?"Water Walk":""}.Where(name=>name.Length>0).ToArray();
            if(d.species=="yuanti")return d.TotalLevel>=3?new[]{"Suggestion"}:Array.Empty<string>();
            if(d.species=="hexblood")return new[]{"Disguise Self","Hex"};
            if(d.species=="firbolg")return new[]{"Detect Magic","Disguise Self"};
            string first=d.species=="fairy"?"Faerie Fire":d.species=="deepgnome"?"Disguise Self":d.species=="duergar"?"Enlarge/Reduce":d.species=="githyanki"?"Jump":d.species=="githzerai"?"Shield":"";
            string second=d.species=="fairy"?"Enlarge/Reduce":d.species=="deepgnome"?"Nondetection":d.species=="duergar"?"Invisibility":d.species=="githyanki"?"Misty Step":d.species=="githzerai"?"Detect Thoughts":"";
            return new[]{d.TotalLevel>=3?first:"",d.TotalLevel>=5?second:""}.Where(name=>name.Length>0).ToArray();
        }
        public static readonly string[] AasimarRevelations={"shroud","consumption","soul"};
        public static readonly string[] ShifterLineages={"beasthide","longtooth","swiftstride","wildhunt"};
        public static readonly string[] EladrinSeasons={"autumn","winter","spring","summer"};
        public static readonly string[] GenasiLineages={"air","earth","fire","water"};
        internal static string LineageName(CharacterCreationDraft d)=>d.species=="aasimarlegacy"&&AasimarRevelations.Contains(d.speciesLineage)?Text("revelation."+d.speciesLineage):d.species=="shifter"&&ShifterLineages.Contains(d.speciesLineage)?Text("shifter."+d.speciesLineage):d.species=="eladrin"&&EladrinSeasons.Contains(d.speciesLineage)?Text("season."+d.speciesLineage):d.species=="genasi"&&GenasiLineages.Contains(d.speciesLineage)?Text("lineage."+d.speciesLineage):"";
        public static string[] Spells(CharacterCreationDraft d)=>(d.species=="rimekin"?new[]{"Ray of Frost"}:d.species=="aasimarlegacy"?new[]{"Light"}:d.species=="khoravar"?new[]{string.IsNullOrEmpty(d.expandedCantrip)?"Friends":d.expandedCantrip}:d.species=="kobold"&&d.expandedGift=="magic"&&!string.IsNullOrEmpty(d.expandedCantrip)?new[]{d.expandedCantrip}:d.species=="genasi"&&GenasiLineages.Contains(d.speciesLineage)?new[]{d.speciesLineage=="air"?"Shocking Grasp":d.speciesLineage=="earth"?"Blade Ward":d.speciesLineage=="fire"?"Produce Flame":"Acid Splash"}:d.species=="astralelf"&&new[]{"Dancing Lights","Light","Sacred Flame"}.Contains(d.expandedCantrip)?new[]{d.expandedCantrip}:d.species=="fairy"?new[]{"Druidcraft"}:d.species=="yuanti"?new[]{"Poison Spray","Animal Friendship"}:d.species=="githyanki"||d.species=="githzerai"?new[]{"Mage Hand"}:Array.Empty<string>()).Concat(FreeSpells(d)).ToArray();
        internal static void Repair(CharacterCreationDraft d)
        {
            if(d.expandedIncreases==null||d.expandedIncreases.Length!=3)d.expandedIncreases=new[]{-1,-1,-1};
            if(d.expandedSkills==null||d.expandedSkills.Length!=2)d.expandedSkills=new[]{-1,-1};
            if(d.expandedTools==null||d.expandedTools.Length!=2)d.expandedTools=new[]{"",""};
            for(int slot=0;slot<2;slot++)if(slot>=ToolCount(d)||!ToolOptions(d,slot).Contains(d.expandedTools[slot]))d.expandedTools[slot]="";
            if(d.simicEnhancements==null||d.simicEnhancements.Length!=2)d.simicEnhancements=new[]{"",""};
            if(d.species!="simic")d.simicEnhancements=new[]{"",""};
            else for(int i=0;i<2;i++)if(i==1&&d.TotalLevel<5||!SimicOptions(d,i).Contains(d.simicEnhancements[i]))d.simicEnhancements[i]="";
            if(!CharacterNaturalArmor.Active(d))d.expandedUnarmored=false;
            d.expandedGift??="";
            if(d.species!="kobold"&&d.species!="khoravar")d.expandedGift="";
            d.expandedInstrument??="";d.expandedTranceProficiency??="";d.expandedSecondTranceProficiency??="";d.expandedCantrip??="";
            if(!Active(d)){d.expandedInstrument=d.expandedSecondTranceProficiency="";d.expandedBonusMode=-1;d.expandedIncreases=new[]{-1,-1,-1};d.expandedSkills=new[]{-1,-1};d.expandedTranceProficiency=d.expandedCantrip="";d.expandedMagicAbility=-1;return;}
            if(!Flexible(d)){d.expandedBonusMode=-1;d.expandedIncreases=new[]{-1,-1,-1};}
            for(int i=0;i<2;i++)if(i>=SkillCount(d)||!SkillOptions(d,i).Contains(d.expandedSkills[i]))d.expandedSkills[i]=-1;
            if(TranceCount(d)==0)d.expandedTranceProficiency=d.expandedSecondTranceProficiency="";
            else if(TranceCount(d)==1)d.expandedSecondTranceProficiency="";
            if(d.species!="khoravar"&&d.species!="astralelf"&&!(d.species=="kobold"&&d.expandedGift=="magic"))d.expandedCantrip="";
            if(!UsesMagic(d)&&d.species!="kender"&&d.species!="eladrin")d.expandedMagicAbility=-1;
            if(d.species!="satyr"||!InstrumentOptions(d).Contains(d.expandedInstrument))d.expandedInstrument="";
        }
        internal static string ChoiceError(CharacterCreationDraft d,int page)
        {
            if(!Active(d))return null;
            if(page==1)
            {
                if(d.species=="aasimarlegacy"&&d.TotalLevel>=3&&!AasimarRevelations.Contains(d.speciesLineage))return Text("error.revelation");
                if(CharacterFtdDragonbornRules.Active(d)&&!CharacterFtdDragonbornRules.Chosen(d))return CharacterFtdDragonbornRules.Text("error.ancestor");
                if(d.species=="simic")
                {
                    if(!new[]{"Ельфійська","Ведалкенська"}.Contains(d.speciesLanguage))return Text("error.simiclanguage");
                    if(d.edition==RulesEdition.Rules2014&&(d.expandedIncreases[0]<0||d.expandedIncreases[0]>5||d.expandedIncreases[0]==2))return Text("error.simicability");
                    if(Enumerable.Range(0,d.TotalLevel>=5?2:1).Any(i=>!SimicOptions(d,i).Contains(d.simicEnhancements[i])))return Text("error.simic");
                }
                if(d.species=="shifter"&&!ShifterLineages.Contains(d.speciesLineage))return Text("error.shifter");
                if(d.species=="kobold"&&!new[]{"craft","defiance","magic"}.Contains(d.expandedGift))return Text("error.gift");
                if(d.species=="kobold"&&d.expandedGift=="magic"&&string.IsNullOrEmpty(d.expandedCantrip))return Text("error.koboldcantrip");
                if(d.species=="eladrin"&&!EladrinSeasons.Contains(d.speciesLineage))return Text("error.season");
                if(d.species=="eladrin"&&!new[]{3,4,5}.Contains(d.expandedMagicAbility))return Text("error.stepability");
                if(d.species=="genasi"&&!GenasiLineages.Contains(d.speciesLineage))return Text("error.genasi");
                if(NeedsSize(d)&&d.speciesSize!="small"&&d.speciesSize!="medium")return Text("error.size");
                if(Flexible(d))
                {
                    int count=d.expandedBonusMode==0?2:3;
                    if(d.expandedBonusMode<0||d.expandedBonusMode>1||d.expandedIncreases.Take(count).Any(i=>i<0||i>5)||d.expandedIncreases.Take(count).Distinct().Count()!=count)return Text("error.increases");
                }
                if(d.species=="astralelf"&&(Spells(d).Length!=1||!new[]{3,4,5}.Contains(d.expandedMagicAbility)))return Text("error.fire");
                if(UsesMagic(d)&&!new[]{3,4,5}.Contains(d.expandedMagicAbility))return Text("error.magic");
                if(d.species=="kender"&&!new[]{3,4,5}.Contains(d.expandedMagicAbility))return Text("error.taunt");
            }
            if(page==3)
            {
                if(d.species=="khoravar"&&!new[]{"skill","tool"}.Contains(d.expandedGift))return Text("error.versatility");
                for(int i=0;i<SkillCount(d);i++)if(!SkillOptions(d,i).Contains(d.expandedSkills[i]))return Text("error.skills");
                if(Enumerable.Range(0,TranceCount(d)).Any(i=>!TranceOptions(d,i).Contains(Trance(d,i)))||Enumerable.Range(0,TranceCount(d)).Select(i=>Trance(d,i)).Distinct().Count()!=TranceCount(d))return Text("error.trance");
                if(d.species=="satyr"&&!InstrumentOptions(d).Contains(d.expandedInstrument))return Text("error.instrument");
                if(ToolCount(d)>0&&Enumerable.Range(0,ToolCount(d)).Any(i=>!ToolOptions(d,i).Contains(d.expandedTools[i])))return Text(d.species=="autognome"?"error.tools":"error.tool");
            }
            return null;
        }
        internal static void AddBonuses(CharacterCreationDraft d,int[] scores)
        {
            if(d.edition!=RulesEdition.Rules2014||!Active(d))return;
            if(d.species=="kalashtar"){scores[4]+=2;scores[5]++;return;}
            if(d.species=="leonin"){scores[2]+=2;scores[0]++;return;}
            if(d.species=="verdan"){scores[5]+=2;scores[2]++;return;}
            if(d.species=="simic"){scores[2]+=2;if(d.expandedIncreases[0]>=0&&d.expandedIncreases[0]<6&&d.expandedIncreases[0]!=2)scores[d.expandedIncreases[0]]++;return;}
            if(d.species=="vedalken"){scores[3]+=2;scores[4]++;return;}
            if(d.species=="locathah"){scores[0]+=2;scores[1]++;return;}
            if(d.species=="loxodon"){scores[2]+=2;scores[4]++;return;}
            int count=d.expandedBonusMode==0?2:d.expandedBonusMode==1?3:0;
            for(int i=0;i<count;i++){int ability=d.expandedIncreases[i];if(ability>=0&&ability<6)scores[ability]+=d.expandedBonusMode==0&&i==0?2:1;}
        }
        public static string Features(CharacterCreationDraft d)
        {
            if(!Active(d))return "";
            if(CharacterFtdDragonbornRules.Active(d))return CharacterFtdDragonbornRules.Features(d);
            string[] keys;
            switch(d.species)
            {
                case "tabaxi":keys=new[]{"tabaxi","darkvision","climb","claws","agility"};break;
                case "kenku":keys=new[]{"kenku","duplication","recall","mimicry"};break;
                case "goblin":keys=new[]{"goblin","darkvision","fey","fury","nimble"};break;
                case "bugbear":keys=new[]{"bugbear","darkvision","fey","longlimbed","powerful","sneaky","surprise"};break;
                case "hobgoblin":keys=new[]{"hobgoblin","darkvision","fey","gift","fortune"};break;
                case "changeling":keys=new[]{"changeling",d.edition==RulesEdition.Rules2024?"shape24":"shape14"};break;
                case "harengon":keys=new[]{"harengon","trigger","footwork","hop"};break;
                case "owlin":keys=new[]{"owlin","darkvision120","wings"};break;
                case "leonin":keys=new[]{"leonin","darkvision","lionclaws","roar"};break;
                case "aarakocra":keys=new[]{"aarakocra","flight","talons","wind"};break;
                case "fairy":keys=new[]{"fairy","flight","fairymagic"};break;
                case "firbolg":keys=new[]{"firbolg","firbolgmagic","hiddenstep","powerful","speech"};break;
                case "giff":keys=new[]{"giff","swim","spark","firearms","hippo"};break;
                case "hadozee":keys=new[]{"hadozee","climb","feet","glide","dodge"};break;
                case "plasmoid":keys=new[]{"plasmoid","darkvision","amorphous","breath","resilience","self","pseudopod"};break;
                case "centaur":keys=new[]{"centaur","charge","equine","hooves"};break;
                case "minotaur":keys=new[]{"minotaur","horns","goring","hammering","labyrinth"};break;
                case "satyr":keys=new[]{"satyr","ram","magicresist","leaps"};break;
                case "seaelf":keys=new[]{"seaelf","darkvision","fey","swim","sea","seafriend","seatrance"};break;
                case "kender":keys=new[]{"kender","fearless","taunt"};break;
                case "verdan":keys=new[]{d.TotalLevel<5?"verdansmall":"verdanmedium","blackblood","telepathy","dual24"};break;
                case "githyanki":keys=new[]{"githyanki","astralknowledge","githyankipsi","psychic"};break;
                case "githzerai":keys=new[]{"githzerai","githzeraipsi","mentaldiscipline","psychic"};break;
                case "hexblood":keys=new[]{"hexblood","darkvision","ancestral","token","tokenmessage","tokenview","hexmagic"};break;
                case "reborn":keys=new[]{"reborn","ancestral","reborndeathless","pastlife"};break;
                case "triton":keys=new[]{"triton","darkvision","swim","amphibious","airwater","emissary","cold"};break;
                case "yuanti":keys=new[]{"yuanti","darkvision","magicresist","poisonresilience","serpentine"};break;
                case "genasi":keys=new[]{"genasi","darkvision",GenasiLineages.Contains(d.speciesLineage)?"genasi"+d.speciesLineage:"genasichoice"};break;
                case "eladrin":keys=new[]{"eladrin","darkvision","fey","eladrinstep","elftrance"};break;
                case "shadarkai":keys=new[]{"shadarkai","darkvision","fey","ravenstep","necrotic","elftrance"};break;
                case "kobold":keys=new[]{"kobold","darkvision","cry",d.expandedGift=="craft"?"craft":d.expandedGift=="magic"?"sorcery":d.expandedGift=="defiance"?"defiance":"giftchoice"};break;
                case "shifter":keys=new[]{d.edition==RulesEdition.Rules2024?"shifter24":"shifter14","darkvision","shifting",ShifterLineages.Contains(d.speciesLineage)?d.speciesLineage:"shifterchoice"};break;
                case "vedalken":keys=new[]{"vedalken","dispassion","precision","partamphibious"};break;
                case "simic":keys=new[]{"simic","darkvision"};break;
                case "khoravar":keys=new[]{"khoravar","darkvision","fey","feygift","lethargy","versatility"};break;
                case "lupin":keys=new[]{"lupin","darkvision","feralpounce","howl"};break;
                case "lorwynchangeling":keys=new[]{"lorwynchangeling","darkvision120","lorwynshape","initmovement"};break;
                case "rimekin":keys=new[]{"rimekin","darkvision","rimecold","coldfire"};break;
                case "orclegacy":keys=new[]{"orclegacy","darkvision","orcadrenaline","powerful","relentless"};break;
                case "goliathlegacy":keys=new[]{"goliathlegacy","powerful","mountain","stoneendurance"};break;
                case "aasimarlegacy":keys=new[]{"aasimarlegacy","darkvision","celestialresistance","healingmpmm","lightbearer"};break;
                case "lizardfolk":keys=new[]{"lizardfolk","swim","lizardbite","breath15","jaws","armor13"};break;
                case "tortle":keys=new[]{"tortle","claws","breathhour","shell17","shelldefense"};break;
                case "locathah":keys=new[]{"locathah","armor12","leviathan","limitedamphibious"};break;
                case "loxodon":keys=new[]{"loxodon","powerful","serenity","armorcon","trunk","smell"};break;
                case "autognome":keys=new[]{"autognome","casing","success","machinehealing","mechanical","sentry"};break;
                case "thrikreen":keys=new[]{"thrikreen","darkvision","carapace","secondaryarms","sleepless","kreentelepathy"};break;
                case "deepgnome":keys=new[]{"deepgnome","darkvision120","svirfmagic","gnomeresistance","camouflage"};break;
                case "duergar":keys=new[]{"duergar","darkvision120","duergarmagic","dwarfresilience","fortitude"};break;
                case "kalashtar":keys=d.edition==RulesEdition.Rules2024?new[]{"kalashtar24","dual24","psychic","mind24","dream24"}:new[]{"kalashtar14","dual14","psychic","mind14","dream14"};break;
                case "dhampir":keys=new[]{"dhampir","darkvision","deathless","climb","bite"};break;
                default:keys=new[]{"astralelf","darkvision","fey","fire","step","trance"};break;
            }
            string result=string.Join("\n",keys.Select(key=>Text("traits."+key)));
            if(d.species=="lupin"&&d.abilities.All(value=>value>0))result+="\n"+string.Format(Text("traits.howldc"),8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2]));
            if(d.species=="eladrin"&&d.TotalLevel>=3&&EladrinSeasons.Contains(d.speciesLineage))
            {
                result+="\n"+Text("traits."+d.speciesLineage);
                if(new[]{3,4,5}.Contains(d.expandedMagicAbility)&&d.abilities.All(v=>v>0))result+="\n"+string.Format(Text("traits.stepdc"),CharacterCreationRules.AbilityNames[d.expandedMagicAbility],8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[d.expandedMagicAbility]));
            }
            if(d.species=="shadarkai"&&d.TotalLevel>=3)result+="\n"+Text("traits.raven3");
            if(d.species=="dhampir"&&d.TotalLevel>=3)result+="\n"+Text("traits.spider3");
            if(d.species=="hobgoblin"&&d.TotalLevel>=3)result+="\n"+Text("traits.hospitality")+"\n"+Text("traits.passage")+"\n"+Text("traits.spite");
            if(d.species=="kender"&&new[]{3,4,5}.Contains(d.expandedMagicAbility)&&d.abilities.All(value=>value>0))result=result.Replace(Text("traits.taunt"),string.Format(Text("traits.tauntvalue"),CharacterCreationRules.AbilityNames[d.expandedMagicAbility],8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[d.expandedMagicAbility])));
            if(d.species=="aasimarlegacy"&&d.TotalLevel>=3&&AasimarRevelations.Contains(d.speciesLineage))
            {
                result+="\n"+Text("traits.revelationmpmm")+"\n"+Text("traits.revelation"+d.speciesLineage);
                if(d.speciesLineage=="shroud"&&d.abilities.All(value=>value>0))result+="\n"+string.Format(Text("traits.shrouddc"),8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[5]));
            }
            if(d.species=="vedalken"&&d.expandedSkills[0]>=0&&d.expandedTools[0].Length>0)result+="\n"+string.Format(Text("traits.precisionvalue"),CharacterCreationRules.SkillNames[d.expandedSkills[0]],d.expandedTools[0]);
            if(d.species=="simic")
            {
                result+="\n"+string.Join("\n",(d.simicEnhancements??Array.Empty<string>()).Take(d.TotalLevel>=5?2:1).Where(SimicAll.Contains).Select(key=>Text("traits.simic"+key)));
                if(Simic(d,"acid")&&d.abilities.All(value=>value>0))result+="\n"+string.Format(Text("traits.acidvalue"),d.TotalLevel>=17?4:d.TotalLevel>=11?3:2,8+CharacterLegacySpeciesRules.Proficiency(d)+CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2]),Math.Max(1,CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2])));
            }
            if(CharacterNaturalArmor.Active(d)&&!CharacterNaturalArmor.WearsArmor(d)&&CharacterNaturalArmor.StarterArmor(d))result+="\n"+Text("traits.armorcarried");
            return result;
        }
        internal static void Apply(CharacterCreationDraft d,CreatedCharacterStats s)
        {
            if(!Active(d))return;
            if(d.species=="dhampir"||d.species=="leonin"||d.species=="satyr")s.speed+=5;
            if(d.species=="centaur")s.speed+=10;
            if(d.species=="genasi"&&d.speciesLineage=="air")s.speed+=5;
            if(d.species=="harengon")s.initiative+=CharacterLegacySpeciesRules.Proficiency(d)-(d.characterClass=="bard"&&d.edition==RulesEdition.Rules2014?CharacterBardRules.HalfProficiency(d):0);
            s.features+="\n"+Features(d);
            if((d.species=="astralelf"||d.species=="githyanki")&&TranceOptions().Contains(d.expandedTranceProficiency))s.proficiencies+="\n"+d.expandedTranceProficiency.Substring(d.expandedTranceProficiency.IndexOf(':')+1)+".";
        }
        public static string Resource(CharacterCreationDraft d)=>d.species=="lupin"?Text("pool.howl"):d.species=="orclegacy"?Text("pool.adrenaline"):d.species=="goliathlegacy"?Text("pool.stone"):d.species=="aasimarlegacy"?Text("pool.hands"):Simic(d,"acid")?Text("pool.acid"):d.species=="vedalken"?Text("pool.breath"):d.species=="lizardfolk"?Text("pool.jaws"):d.species=="autognome"?Text("pool.success"):d.species=="shifter"?Text("pool.shifting"):d.species=="kobold"?Text("pool.cry"):d.species=="eladrin"?Text("pool.feystep"):d.species=="shadarkai"?Text("pool.raven"):d.species=="genasi"&&d.speciesLineage=="earth"?Text("pool.earthward"):d.species=="hexblood"?Text("pool.token"):d.species=="reborn"?Text("pool.pastlife"):d.species=="deepgnome"?Text("pool.camouflage"):d.species=="kender"?Text("pool.taunt"):d.species=="giff"?Text("pool.spark"):d.species=="hadozee"?Text("pool.dodge"):d.species=="firbolg"?Text("pool.hidden"):d.species=="leonin"?Text("pool.roar"):d.species=="harengon"?Text("pool.hop"):d.species=="hobgoblin"?Text("pool.fortune"):d.species=="goblin"?Text("pool.fury"):d.species=="kenku"?Text("pool.recall"):d.species=="dhampir"?Text("pool.bite"):d.species=="astralelf"?Text("pool.step"):"";
        public static int ResourceUses(CharacterCreationDraft d)=>Simic(d,"acid")?Math.Max(1,CharacterCreationRules.Modifier(CharacterCreationRules.Scores(d)[2])):d.species=="aasimarlegacy"||d.species=="vedalken"||d.species=="leonin"||d.species=="hexblood"?1:CharacterLegacySpeciesRules.Proficiency(d);
        public static bool ShortRestResource(CharacterCreationDraft d)=>d.species=="leonin";
    }
}
