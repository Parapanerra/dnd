import json
from pathlib import Path
root=Path(__file__).resolve().parents[2]
data=json.loads((Path(__file__).parent/'cache/data/class/class-cleric.json').read_text(encoding='utf-8'))
spells=json.loads((root/'Assets/Resources/CharacterCreation/SpellIndexLevel01.json').read_text(encoding='utf-8-sig'))['entries']
names={(s['edition'],s['name'].lower()):s['name'] for s in spells}
q=lambda s:json.dumps(s,ensure_ascii=False)
lines=['// Generated from the pinned PHB/XPHB Cleric additionalSpells tables.', 'using System;', 'using System.Linq;', 'namespace Taruck.CharacterCreation { public static partial class CharacterClericRules {', 'public static string[] DomainSpells(CharacterCreationDraft d) {', 'if(!Active(d)||d.level<SubclassLevel(d))return Array.Empty<string>();', 'switch((Revised(d)?"2024.":"2014.")+d.subclass) {']
for sub in data['subclass']:
    if sub['source'] not in ('PHB','XPHB') or sub['source']!=sub['classSource']:continue
    year=2014 if sub['source']=='PHB' else 2024
    lines+=['case '+q(str(year)+'.'+sub['shortName'].lower())+': return new string[][] {']
    levels=[]
    for level,values in sub['additionalSpells'][0]['prepared'].items():
        levels.append(int(level));values=[names[year,s.split('|')[0].lower()] for s in values]
        lines+=['new [] {'+','.join(q(s) for s in values)+'},']
    lines+=['}.Where((values,index)=>d.level>=new[]{'+','.join(map(str,levels))+'}[index]).SelectMany(values=>values).ToArray();']
lines+=['default:return Array.Empty<string>();','} } } }']
(root/'Assets/Skripts/Domain/CharacterClericDomainSpells.cs').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print('Generated eleven edition-specific domain spell tables.')
