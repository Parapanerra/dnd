import json
from pathlib import Path
root = Path(__file__).resolve().parents[2]
source = Path(__file__).parent
texts = {}; translations = []
for line in (source/'sorcerer-spells.tsv').read_text(encoding='utf-8-sig').splitlines():
    if not line: continue
    name, ua_name, en_name, ru_name, ua, en, ru = line.split('\t')
    texts[name] = {'name':name, 'classic':ua, 'revised':ua}
    translations.extend(((ua_name,en_name,ru_name),(ua,en,ru)))
for line in (source/'sorcerer-spells-classic.tsv').read_text(encoding='utf-8-sig').splitlines():
    if not line: continue
    name, ua, en, ru = line.split('\t')
    if name not in texts: texts[name] = {'name':name,'classic':'','revised':''}
    texts[name]['classic'] = ua
    translations.append((ua,en,ru))
for line in (source/'sorcerer-spells-modern.tsv').read_text(encoding='utf-8-sig').splitlines():
    if not line: continue
    name, ua, en, ru = line.split('\t')
    if name not in texts: texts[name] = {'name':name,'classic':'','revised':''}
    texts[name]['revised'] = ua
    translations.append((ua,en,ru))
(root/'Assets/Resources/CharacterCreation/SorcererSpellText.json').write_text(json.dumps({'entries':list(texts.values())},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
p=root/'Assets/Resources/Localization.txt';existing=p.read_text(encoding='utf-8-sig');known={line.split('\t')[0] for line in existing.splitlines()}; added=[]
for row in translations:
    if row[0] not in known: added.append('\t'.join(row));known.add(row[0])
p.write_text(existing.rstrip('\n')+'\n'+'\n'.join(added)+'\n',encoding='utf-8')
print(f'Built {len(texts)} spell texts, {len(added)} translations.')
