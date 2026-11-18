import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
data=json.loads((Path(__file__).parent/'cache/data/class/class-druid.json').read_text(encoding='utf-8'))
index=json.loads((root/'Assets/Resources/CharacterCreation/SpellIndexLevel01.json').read_text(encoding='utf-8-sig'))['entries']
lookup={(x['edition'],x['name'].lower()):x['name'] for x in index}
q=lambda s:json.dumps(s,ensure_ascii=False)
lines=['// Generated from pinned PHB/XPHB Druid additionalSpells.','using System;using System.Linq;','namespace Taruck.CharacterCreation {public static partial class CharacterDruidRules {','internal static string[] CircleSpells(CharacterCreationDraft d) {','if(!Active(d)||d.level<SubclassLevel(d))return Array.Empty<string>();','switch((Revised(d)?"2024.":"2014.")+d.subclass+(Is(d,"land")?"."+d.druidLand:"")){']
for sub in data['subclass']:
 if sub['source'] not in ('PHB','XPHB') or sub['source']!=sub['classSource']:continue
 year=2014 if sub['source']=='PHB' else 2024
 for entry in sub.get('additionalSpells',[]):
  if not entry.get('prepared'):continue
  suffix='.'+entry['name'].lower().replace(' land','') if sub['shortName']=='Land' else ''
  levels=[];groups=[]
  for lv,values in entry['prepared'].items():
   levels.append(int(lv));groups.append([lookup[year,v.split('|')[0].split('#')[0].lower()] for v in values])
  lines+=['case '+q(str(year)+'.'+sub['shortName'].lower()+suffix)+':return new string[][]{'+','.join('new[]{'+','.join(map(q,g))+'}' for g in groups)+'}.Where((values,i)=>d.level>=new[]{'+','.join(map(str,levels))+'}[i]).SelectMany(v=>v).ToArray();']
lines+=['default:return Array.Empty<string>();','}}}}']
(root/'Assets/Skripts/Domain/CharacterDruidCircleSpells.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Generated Druid circle spell tables.')
