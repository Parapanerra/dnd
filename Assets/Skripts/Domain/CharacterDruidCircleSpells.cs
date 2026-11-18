// Generated from pinned PHB/XPHB Druid additionalSpells.
using System;using System.Linq;
namespace Taruck.CharacterCreation {public static partial class CharacterDruidRules {
internal static string[] CircleSpells(CharacterCreationDraft d) {
if(!Active(d)||d.level<SubclassLevel(d))return Array.Empty<string>();
switch((Revised(d)?"2024.":"2014.")+d.subclass+(Is(d,"land")?"."+d.druidLand:"")){
case "2014.land.arctic":return new string[][]{new[]{"Hold Person","Spike Growth"},new[]{"Sleet Storm","Slow"},new[]{"Freedom of Movement","Ice Storm"},new[]{"Commune with Nature","Cone of Cold"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.coast":return new string[][]{new[]{"Mirror Image","Misty Step"},new[]{"Water Breathing","Water Walk"},new[]{"Control Water","Freedom of Movement"},new[]{"Conjure Elemental","Scrying"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.desert":return new string[][]{new[]{"Blur","Silence"},new[]{"Create Food and Water","Protection from Energy"},new[]{"Blight","Hallucinatory Terrain"},new[]{"Insect Plague","Wall of Stone"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.forest":return new string[][]{new[]{"Barkskin","Spider Climb"},new[]{"Call Lightning","Plant Growth"},new[]{"Divination","Freedom of Movement"},new[]{"Commune with Nature","Tree Stride"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.grassland":return new string[][]{new[]{"Invisibility","Pass without Trace"},new[]{"Daylight","Haste"},new[]{"Divination","Freedom of Movement"},new[]{"Dream","Insect Plague"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.mountain":return new string[][]{new[]{"Spider Climb","Spike Growth"},new[]{"Lightning Bolt","Meld into Stone"},new[]{"Stone Shape","Stoneskin"},new[]{"Passwall","Wall of Stone"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.swamp":return new string[][]{new[]{"Darkness","Melf's Acid Arrow"},new[]{"Water Walk","Stinking Cloud"},new[]{"Freedom of Movement","Locate Creature"},new[]{"Insect Plague","Scrying"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2014.land.underdark":return new string[][]{new[]{"Spider Climb","Web"},new[]{"Gaseous Form","Stinking Cloud"},new[]{"Greater Invisibility","Stone Shape"},new[]{"Cloudkill","Insect Plague"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.land.arid":return new string[][]{new[]{"Blur","Burning Hands","Fire Bolt"},new[]{"Fireball"},new[]{"Blight"},new[]{"Wall of Stone"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.land.polar":return new string[][]{new[]{"Fog Cloud","Hold Person","Ray of Frost"},new[]{"Sleet Storm"},new[]{"Ice Storm"},new[]{"Cone of Cold"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.land.temperate":return new string[][]{new[]{"Misty Step","Shocking Grasp","Sleep"},new[]{"Lightning Bolt"},new[]{"Freedom of Movement"},new[]{"Tree Stride"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.land.tropical":return new string[][]{new[]{"Acid Splash","Ray of Sickness","Web"},new[]{"Stinking Cloud"},new[]{"Polymorph"},new[]{"Insect Plague"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.moon":return new string[][]{new[]{"Cure Wounds","Moonbeam","Starry Wisp"},new[]{"Conjure Animals"},new[]{"Fount of Moonlight"},new[]{"Mass Cure Wounds"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.sea":return new string[][]{new[]{"Fog Cloud","Gust of Wind","Ray of Frost","Shatter","Thunderwave"},new[]{"Lightning Bolt","Water Breathing"},new[]{"Control Water","Ice Storm"},new[]{"Conjure Elemental","Hold Monster"}}.Where((values,i)=>d.level>=new[]{3,5,7,9}[i]).SelectMany(v=>v).ToArray();
case "2024.stars":return new string[][]{new[]{"Guiding Bolt"}}.Where((values,i)=>d.level>=new[]{3}[i]).SelectMany(v=>v).ToArray();
default:return Array.Empty<string>();
}}}}
