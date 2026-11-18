from pathlib import Path
import json
p=Path(__file__).resolve().parents[2]
ids=dict(zip(['Agonizing Blast','Armor of Shadows','Ascendant Step','Beast Speech','Beguiling Influence','Bewitching Whispers','Book of Ancient Secrets','Chains of Carceri',"Devil's Sight",'Devouring Blade','Dreadful Word','Eldritch Mind','Eldritch Sight','Eldritch Smite','Eldritch Spear','Eyes of the Rune Keeper','Fiendish Vigor','Gaze of Two Minds','Gift of the Depths','Gift of the Protectors','Investment of the Chain Master','Lessons of the First Ones','Lifedrinker','Mask of Many Faces','Master of Myriad Forms','Minions of Chaos','Mire the Mind','Misty Visions','One with Shadows','Otherworldly Leap','Pact of the Blade','Pact of the Chain','Pact of the Tome','Repelling Blast','Sculptor of Flesh','Sign of Ill Omen','Thief of Five Fates','Thirsting Blade','Visions of Distant Realms','Voice of the Chain Master','Whispers of the Grave','Witch Sight'],['agonizing','armor','ascendant','beast','beguiling','whispers','ancient','carceri','devil','devouring','word','mind','sight','smite','spear','runes','vigor','gaze','depths','protectors','investment','lessons','lifedrinker','mask','forms','chaos','mire','visions','shadows','leap','blade','chain','tome','repelling','sculptor','omen','fates','thirsting','realms','voice','grave','witch']))
spells={'armor':'Mage Armor','ascendant':'Levitate','beast':'Speak with Animals','whispers':'Compulsion','carceri':'Hold Monster','word':'Confusion','sight':'Detect Magic','vigor':'False Life','depths':'Water Breathing','mask':'Disguise Self','forms':'Alter Self','chaos':'Conjure Elemental','mire':'Slow','visions':'Silent Image','leap':'Jump','chain':'Find Familiar','sculptor':'Polymorph','omen':'Bestow Curse','fates':'Bane','realms':'Arcane Eye','grave':'Speak with Dead'}
raw=json.loads((p/'tools/character-data/cache/data/optionalfeatures.json').read_text())
q=lambda x:json.dumps(x,ensure_ascii=False)
lines=['// Reviewed PHB/XPHB invocation metadata from the pinned local snapshot.','using System; namespace Taruck.CharacterCreation { public static class CharacterWarlockInvocationData { public static readonly CharacterWarlockInvocations.Definition[] All={']
for x in raw['optionalfeature']:
 if x['source'] not in ['PHB','XPHB'] or not any(t.startswith('EI') for t in x.get('featureType',[])):continue
 id=ids[x['name']];modern=x['source']=='XPHB';pr=x.get('prerequisite',[{}])[0];minimum=pr.get('level',{}).get('level',1 if modern else 2);dep=ids.get(pr.get('optionalfeature',[''])[0].split('|')[0].title(),'')
 # title() changes articles; resolve case-insensitively.
 dep=next((v for k,v in ids.items() if k.lower()==pr.get('optionalfeature',[''])[0].split('|')[0].lower()),'')
 spell='Invisibility' if id=='shadows' and modern else spells.get(id,'')
 lines.append('new CharacterWarlockInvocations.Definition('+','.join([q(id),q(x['name']),str(minimum),str(modern).lower(),q(dep),q(pr.get('pact','').lower()),q(spell)])+'),')
lines+=['}; internal static bool CantripEligible(int edition,string name,string invocation){switch(edition+"|"+name){']
for src,ed in [('phb',2014),('xphb',2024)]:
 for x in json.loads((p/f'tools/character-data/cache/data/spells/spells-{src}.json').read_text())['spell']:
  if x.get('level')!=0:continue
  damage=bool(x.get('damageInflict'));attack=bool(x.get('spellAttack'));rng=x.get('range',{}).get('distance',{});ranged=rng.get('type')=='feet' and rng.get('amount',0)>=10
  if damage or attack:lines.append('case '+q(str(ed)+'|'+x['name'])+': return invocation=="repelling"?'+str(attack).lower()+':invocation=="spear"?'+str(damage and ranged).lower()+':'+str(damage).lower()+';')
lines+=['default:return false;}}}}']
(p/'Assets/Skripts/Domain/CharacterWarlockInvocationData.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Invocation definitions:',sum('new CharacterWarlock' in l for l in lines))
