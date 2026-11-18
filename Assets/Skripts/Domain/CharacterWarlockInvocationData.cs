// Reviewed PHB/XPHB invocation metadata from the pinned local snapshot.
using System; namespace Taruck.CharacterCreation { public static class CharacterWarlockInvocationData { public static readonly CharacterWarlockInvocations.Definition[] All={
new CharacterWarlockInvocations.Definition("agonizing","Agonizing Blast",2,false,"","",""),
new CharacterWarlockInvocations.Definition("agonizing","Agonizing Blast",2,true,"","",""),
new CharacterWarlockInvocations.Definition("armor","Armor of Shadows",2,false,"","","Mage Armor"),
new CharacterWarlockInvocations.Definition("armor","Armor of Shadows",1,true,"","","Mage Armor"),
new CharacterWarlockInvocations.Definition("ascendant","Ascendant Step",9,false,"","","Levitate"),
new CharacterWarlockInvocations.Definition("ascendant","Ascendant Step",5,true,"","","Levitate"),
new CharacterWarlockInvocations.Definition("beast","Beast Speech",2,false,"","","Speak with Animals"),
new CharacterWarlockInvocations.Definition("beguiling","Beguiling Influence",2,false,"","",""),
new CharacterWarlockInvocations.Definition("whispers","Bewitching Whispers",7,false,"","","Compulsion"),
new CharacterWarlockInvocations.Definition("ancient","Book of Ancient Secrets",2,false,"","tome",""),
new CharacterWarlockInvocations.Definition("carceri","Chains of Carceri",15,false,"","chain","Hold Monster"),
new CharacterWarlockInvocations.Definition("devil","Devil's Sight",2,false,"","",""),
new CharacterWarlockInvocations.Definition("devil","Devil's Sight",2,true,"","",""),
new CharacterWarlockInvocations.Definition("devouring","Devouring Blade",12,true,"thirsting","",""),
new CharacterWarlockInvocations.Definition("word","Dreadful Word",7,false,"","","Confusion"),
new CharacterWarlockInvocations.Definition("mind","Eldritch Mind",1,true,"","",""),
new CharacterWarlockInvocations.Definition("sight","Eldritch Sight",2,false,"","","Detect Magic"),
new CharacterWarlockInvocations.Definition("smite","Eldritch Smite",5,true,"blade","",""),
new CharacterWarlockInvocations.Definition("spear","Eldritch Spear",2,false,"","",""),
new CharacterWarlockInvocations.Definition("spear","Eldritch Spear",2,true,"","",""),
new CharacterWarlockInvocations.Definition("runes","Eyes of the Rune Keeper",2,false,"","",""),
new CharacterWarlockInvocations.Definition("vigor","Fiendish Vigor",2,false,"","","False Life"),
new CharacterWarlockInvocations.Definition("vigor","Fiendish Vigor",2,true,"","","False Life"),
new CharacterWarlockInvocations.Definition("gaze","Gaze of Two Minds",2,false,"","",""),
new CharacterWarlockInvocations.Definition("gaze","Gaze of Two Minds",5,true,"","",""),
new CharacterWarlockInvocations.Definition("depths","Gift of the Depths",5,true,"","","Water Breathing"),
new CharacterWarlockInvocations.Definition("protectors","Gift of the Protectors",9,true,"tome","",""),
new CharacterWarlockInvocations.Definition("investment","Investment of the Chain Master",5,true,"chain","",""),
new CharacterWarlockInvocations.Definition("lessons","Lessons of the First Ones",2,true,"","",""),
new CharacterWarlockInvocations.Definition("lifedrinker","Lifedrinker",12,false,"","blade",""),
new CharacterWarlockInvocations.Definition("lifedrinker","Lifedrinker",9,true,"blade","",""),
new CharacterWarlockInvocations.Definition("mask","Mask of Many Faces",2,false,"","","Disguise Self"),
new CharacterWarlockInvocations.Definition("mask","Mask of Many Faces",2,true,"","","Disguise Self"),
new CharacterWarlockInvocations.Definition("forms","Master of Myriad Forms",15,false,"","","Alter Self"),
new CharacterWarlockInvocations.Definition("forms","Master of Myriad Forms",5,true,"","","Alter Self"),
new CharacterWarlockInvocations.Definition("chaos","Minions of Chaos",9,false,"","","Conjure Elemental"),
new CharacterWarlockInvocations.Definition("mire","Mire the Mind",5,false,"","","Slow"),
new CharacterWarlockInvocations.Definition("visions","Misty Visions",2,false,"","","Silent Image"),
new CharacterWarlockInvocations.Definition("visions","Misty Visions",2,true,"","","Silent Image"),
new CharacterWarlockInvocations.Definition("shadows","One with Shadows",5,false,"","",""),
new CharacterWarlockInvocations.Definition("shadows","One with Shadows",5,true,"","","Invisibility"),
new CharacterWarlockInvocations.Definition("leap","Otherworldly Leap",9,false,"","","Jump"),
new CharacterWarlockInvocations.Definition("leap","Otherworldly Leap",2,true,"","","Jump"),
new CharacterWarlockInvocations.Definition("blade","Pact of the Blade",1,true,"","",""),
new CharacterWarlockInvocations.Definition("chain","Pact of the Chain",1,true,"","","Find Familiar"),
new CharacterWarlockInvocations.Definition("tome","Pact of the Tome",1,true,"","",""),
new CharacterWarlockInvocations.Definition("repelling","Repelling Blast",2,false,"","",""),
new CharacterWarlockInvocations.Definition("repelling","Repelling Blast",2,true,"","",""),
new CharacterWarlockInvocations.Definition("sculptor","Sculptor of Flesh",7,false,"","","Polymorph"),
new CharacterWarlockInvocations.Definition("omen","Sign of Ill Omen",5,false,"","","Bestow Curse"),
new CharacterWarlockInvocations.Definition("fates","Thief of Five Fates",2,false,"","","Bane"),
new CharacterWarlockInvocations.Definition("thirsting","Thirsting Blade",5,false,"","blade",""),
new CharacterWarlockInvocations.Definition("thirsting","Thirsting Blade",5,true,"blade","",""),
new CharacterWarlockInvocations.Definition("realms","Visions of Distant Realms",15,false,"","","Arcane Eye"),
new CharacterWarlockInvocations.Definition("realms","Visions of Distant Realms",9,true,"","","Arcane Eye"),
new CharacterWarlockInvocations.Definition("voice","Voice of the Chain Master",2,false,"","chain",""),
new CharacterWarlockInvocations.Definition("grave","Whispers of the Grave",9,false,"","","Speak with Dead"),
new CharacterWarlockInvocations.Definition("grave","Whispers of the Grave",7,true,"","","Speak with Dead"),
new CharacterWarlockInvocations.Definition("witch","Witch Sight",15,false,"","",""),
new CharacterWarlockInvocations.Definition("witch","Witch Sight",15,true,"","",""),
}; internal static bool CantripEligible(int edition,string name,string invocation){switch(edition+"|"+name){
case "2014|Acid Splash": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2014|Chill Touch": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2014|Eldritch Blast": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2014|Fire Bolt": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2014|Poison Spray": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2014|Produce Flame": return invocation=="repelling"?true:invocation=="spear"?false:true;
case "2014|Ray of Frost": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2014|Sacred Flame": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2014|Shocking Grasp": return invocation=="repelling"?true:invocation=="spear"?false:true;
case "2014|Thorn Whip": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2014|Vicious Mockery": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|Acid Splash": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|Chill Touch": return invocation=="repelling"?true:invocation=="spear"?false:true;
case "2024|Eldritch Blast": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Fire Bolt": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Mind Sliver": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|Poison Spray": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Produce Flame": return invocation=="repelling"?true:invocation=="spear"?false:true;
case "2024|Ray of Frost": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Sacred Flame": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|Shillelagh": return invocation=="repelling"?false:invocation=="spear"?false:true;
case "2024|Shocking Grasp": return invocation=="repelling"?true:invocation=="spear"?false:true;
case "2024|Sorcerous Burst": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Starry Wisp": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Thorn Whip": return invocation=="repelling"?true:invocation=="spear"?true:true;
case "2024|Thunderclap": return invocation=="repelling"?false:invocation=="spear"?false:true;
case "2024|Toll the Dead": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|True Strike": return invocation=="repelling"?false:invocation=="spear"?false:true;
case "2024|Vicious Mockery": return invocation=="repelling"?false:invocation=="spear"?true:true;
case "2024|Word of Radiance": return invocation=="repelling"?false:invocation=="spear"?false:true;
default:return false;}}}}
