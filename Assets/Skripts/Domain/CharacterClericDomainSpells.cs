// Generated from the pinned PHB/XPHB Cleric additionalSpells tables.
using System;
using System.Linq;
namespace Taruck.CharacterCreation { public static partial class CharacterClericRules {
public static string[] DomainSpells(CharacterCreationDraft d) {
if(!Active(d)||d.level<SubclassLevel(d))return Array.Empty<string>();
switch((Revised(d)?"2024.":"2014.")+d.subclass) {
case "2014.knowledge": return new string[][] {
new [] {"Command","Identify"},
new [] {"Augury","Suggestion"},
new [] {"Nondetection","Speak with Dead"},
new [] {"Arcane Eye","Confusion"},
new [] {"Legend Lore","Scrying"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.life": return new string[][] {
new [] {"Bless","Cure Wounds"},
new [] {"Lesser Restoration","Spiritual Weapon"},
new [] {"Beacon of Hope","Revivify"},
new [] {"Death Ward","Guardian of Faith"},
new [] {"Mass Cure Wounds","Raise Dead"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.light": return new string[][] {
new [] {"Burning Hands","Faerie Fire"},
new [] {"Flaming Sphere","Scorching Ray"},
new [] {"Daylight","Fireball"},
new [] {"Guardian of Faith","Wall of Fire"},
new [] {"Flame Strike","Scrying"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.nature": return new string[][] {
new [] {"Animal Friendship","Speak with Animals"},
new [] {"Barkskin","Spike Growth"},
new [] {"Plant Growth","Wind Wall"},
new [] {"Dominate Beast","Grasping Vine"},
new [] {"Insect Plague","Tree Stride"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.tempest": return new string[][] {
new [] {"Fog Cloud","Thunderwave"},
new [] {"Gust of Wind","Shatter"},
new [] {"Call Lightning","Sleet Storm"},
new [] {"Control Water","Ice Storm"},
new [] {"Destructive Wave","Insect Plague"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.trickery": return new string[][] {
new [] {"Charm Person","Disguise Self"},
new [] {"Mirror Image","Pass without Trace"},
new [] {"Blink","Dispel Magic"},
new [] {"Dimension Door","Polymorph"},
new [] {"Dominate Person","Modify Memory"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2014.war": return new string[][] {
new [] {"Divine Favor","Shield of Faith"},
new [] {"Magic Weapon","Spiritual Weapon"},
new [] {"Crusader's Mantle","Spirit Guardians"},
new [] {"Freedom of Movement","Stoneskin"},
new [] {"Flame Strike","Hold Monster"},
}.Where((values,index)=>d.level>=new[]{1,3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2024.life": return new string[][] {
new [] {"Aid","Bless","Cure Wounds","Lesser Restoration"},
new [] {"Mass Healing Word","Revivify"},
new [] {"Aura of Life","Death Ward"},
new [] {"Greater Restoration","Mass Cure Wounds"},
}.Where((values,index)=>d.level>=new[]{3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2024.light": return new string[][] {
new [] {"Burning Hands","Faerie Fire","Scorching Ray","See Invisibility"},
new [] {"Daylight","Fireball"},
new [] {"Arcane Eye","Wall of Fire"},
new [] {"Flame Strike","Scrying"},
}.Where((values,index)=>d.level>=new[]{3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2024.trickery": return new string[][] {
new [] {"Charm Person","Disguise Self","Invisibility","Pass without Trace"},
new [] {"Hypnotic Pattern","Nondetection"},
new [] {"Confusion","Dimension Door"},
new [] {"Dominate Person","Modify Memory"},
}.Where((values,index)=>d.level>=new[]{3,5,7,9}[index]).SelectMany(values=>values).ToArray();
case "2024.war": return new string[][] {
new [] {"Guiding Bolt","Magic Weapon","Shield of Faith","Spiritual Weapon"},
new [] {"Crusader's Mantle","Spirit Guardians"},
new [] {"Fire Shield","Freedom of Movement"},
new [] {"Hold Monster","Steel Wind Strike"},
}.Where((values,index)=>d.level>=new[]{3,5,7,9}[index]).SelectMany(values=>values).ToArray();
default:return Array.Empty<string>();
} } } }
